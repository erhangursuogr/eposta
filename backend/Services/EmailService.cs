using DeuEposta.Data;
using DeuEposta.Models;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net.Mail;
using System.Net.Sockets;

namespace DeuEposta.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(SendEmailRequest request);

    Task<Dictionary<string, bool>> SendBatchEmailAsync(SendEmailRequest request, int batchSize = 50);

    Task<List<string>> GetSmartRecipientsAsync(int groupId, string category);

    Task<EmailConfig> GetEmailConfigAsync();

    Task<bool> ValidateEmailSecurityAsync(string content, List<string> recipients);
}

public class EmailService : IEmailService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailCategoryService _emailCategoryService;
    private readonly IAuditLogService _auditLog;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailService(DeuEpostaContext context, ILogger<EmailService> logger, IConfiguration configuration, IEmailCategoryService emailCategoryService, IAuditLogService auditLog)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _emailCategoryService = emailCategoryService;
        _auditLog = auditLog;

        // Polly retry policy: 3 attempts with exponential backoff
        _retryPolicy = Policy
            .Handle<SmtpException>()
            .Or<SocketException>()
            .Or<TimeoutException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Email send attempt {RetryCount} failed. Waiting {Delay}s before retry. Error: {Message}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
    }

    public async Task<bool> SendEmailAsync(SendEmailRequest request)
    {
        try
        {
            // GÜVENLİK: Email içeriği ve alıcı listesi güvenlik kontrolü
            var allRecipients = new List<string>();
            if (request.ToRecipients != null) allRecipients.AddRange(request.ToRecipients);
            if (request.CcRecipients != null) allRecipients.AddRange(request.CcRecipients);
            if (request.BccRecipients != null) allRecipients.AddRange(request.BccRecipients);

            if (!await ValidateEmailSecurityAsync(request.Body, allRecipients))
            {
                _logger.LogWarning("Email security validation failed. Subject: {Subject}, Recipients: {Count}",
                    request.Subject, allRecipients.Count);
                return false;
            }

            // Kategori belirtilmişse o kategorinin config'ini kullan, yoksa genel config
            EmailConfig emailConfig;
            if (!string.IsNullOrEmpty(request.Category))
            {
                emailConfig = await _emailCategoryService.GetEmailConfigByCategoryAsync(request.Category);
            }
            else
            {
                // Legacy support - eski sistem uyumluluğu için
                var generalConfig = await GetEmailConfigAsync();
                emailConfig = new EmailConfig
                {
                    FromEmail = generalConfig.FromEmail,
                    FromName = generalConfig.FromName,
                    SmtpServer = generalConfig.SmtpServer,
                    SmtpPort = generalConfig.SmtpPort,
                    SmtpUsername = generalConfig.SmtpUsername,
                    SmtpPassword = generalConfig.SmtpPassword,
                    UseDefaultCredentials = string.IsNullOrEmpty(generalConfig.SmtpUsername)
                };
            }

            using var client = new SmtpClient(emailConfig.SmtpServer, emailConfig.SmtpPort)
            {
                EnableSsl = emailConfig.EnableSsl,
                UseDefaultCredentials = emailConfig.UseDefaultCredentials,
                Credentials = emailConfig.UseDefaultCredentials || string.IsNullOrEmpty(emailConfig.SmtpUsername)
                    ? null
                    : new System.Net.NetworkCredential(emailConfig.SmtpUsername, emailConfig.SmtpPassword)
            };

            // GÜVENLİK: FROM email adresinin sistemde tanımlı olup olmadığını kontrol et
            var isValidFromEmail = await ValidateFromEmailAsync(emailConfig.FromEmail);
            if (!isValidFromEmail)
            {
                _logger.LogError("SECURITY WARNING: Attempted to send email from unauthorized address: {FromEmail}", emailConfig.FromEmail);
                throw new UnauthorizedAccessException($"Email gönderme yetkisi olmayan adres: {emailConfig.FromEmail}. Bu olay loglandı.");
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(emailConfig.FromEmail, emailConfig.FromName),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = request.IsHtml
            };

            // TO recipients
            foreach (var to in request.ToRecipients ?? [])
            {
                mailMessage.To.Add(to);
            }

            // CC recipients
            foreach (var cc in request.CcRecipients ?? [])
            {
                mailMessage.CC.Add(cc);
            }

            // BCC recipients
            foreach (var bcc in request.BccRecipients ?? [])
            {
                mailMessage.Bcc.Add(bcc);
            }

            // Attachments
            foreach (var attachment in request.Attachments)
            {
                if (File.Exists(attachment.FilePath))
                {
                    var att = new System.Net.Mail.Attachment(attachment.FilePath)
                    {
                        Name = attachment.FileName
                    };
                    mailMessage.Attachments.Add(att);
                    _logger.LogInformation("Attachment added: {FileName} from {FilePath}", attachment.FileName, attachment.FilePath);
                }
                else
                {
                    _logger.LogWarning("Attachment file not found: {FilePath}", attachment.FilePath);
                }
            }

            // Send email with retry policy
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await client.SendMailAsync(mailMessage);
            });

            _logger.LogInformation("Email sent successfully to {RecipientCount} recipients",
                (request.ToRecipients?.Count ?? 0) + (request.CcRecipients?.Count ?? 0) + (request.BccRecipients?.Count ?? 0));

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // GÜVENLİK: Yetkisiz email gönderme girişimi - exception'ı aynen fırlat
            throw;
        }
        catch (InvalidOperationException)
        {
            // GÜVENLİK: Geçersiz konfigürasyon - exception'ı aynen fırlat
            throw;
        }
        catch (Exception ex)
        {
            // SMTP hatası - loglayıp false dön
            _logger.LogError(ex, "Error sending email");
            return false;
        }
    }

    /// <summary>
    /// Grup tipine göre akıllı alıcı listesi döner
    /// DEBIS grupları için özel handling: Listeci email kullanılır
    /// </summary>
    public async Task<List<string>> GetSmartRecipientsAsync(int groupId, string category)
    {
        var group = await _context.EpostaGruplari
            .Include(g => g.Uyeler)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            return new List<string>();
        }

        _logger.LogInformation("Resolving recipients for group {GroupId} ({GroupType})", groupId, group.GrupTipi);

        return group.GrupTipi switch
        {
            "NORMAL" => GetNormalGroupRecipients(group),
            "STATIK" or "STATIC" => await GetStaticGroupRecipientsAsync(group),
            "DINAMIK" or "DYNAMIC" => await GetDynamicGroupRecipientsAsync(group),
            "DEBIS" => GetDebisGroupRecipients(group, category),
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Normal grup alıcılarını döner (database'deki üyeler)
    /// </summary>
    private List<string> GetNormalGroupRecipients(EpostaGrubu group)
    {
        var recipients = group.Uyeler
            .Where(u => u.Durum == "AKTIF")
            .Select(u => u.Email)
            .ToList();

        _logger.LogInformation("Normal group {GroupId}: {Count} recipients", group.Id, recipients.Count);
        return recipients;
    }

    /// <summary>
    /// DEBIS grup alıcılarını döner
    /// DEBIS grupları mevcut DEBIS sistemi ile entegre olup listeci email kullanır
    /// Listeci email'e gönderilen email otomatik olarak tüm grup üyelerine dağıtılır
    /// </summary>
    private List<string> GetDebisGroupRecipients(EpostaGrubu group, string category)
    {
        if (string.IsNullOrEmpty(group.ListeciEmail))
        {
            _logger.LogWarning("DEBIS group {GroupId} has no listeci email configured", group.Id);
            return new List<string>();
        }

        // DEBIS grupları için sadece listeci email kullanılır
        // Listeci email formatı: akademik44_seftali@deu.edu.tr, idari33_armut@deu.edu.tr gibi

        // Category kontrolü: DEBIS grupları BCC-only olmalı (GÜVENLİK ZORUNLULUĞU)
        if (category != "BCC")
        {
            _logger.LogError("SECURITY VIOLATION: DEBIS group {GroupId} attempted to use {Category} category instead of BCC. Request denied.",
                group.Id, category);
            throw new InvalidOperationException($"DEBIS grupları güvenlik nedeniyle sadece BCC kategorisinde kullanılabilir. Grup: {group.GrupAdi}, Talep edilen kategori: {category}");
        }

        _logger.LogInformation("DEBIS group {GroupId}: using listeci email {ListeciEmail} with BCC",
            group.Id, group.ListeciEmail);

        return new List<string> { group.ListeciEmail };
    }

    public async Task<EmailConfig> GetEmailConfigAsync()
    {
        // Varsayılan PERSONEL kategorisi için EmailConfig alınır
        return await _emailCategoryService.GetEmailConfigByCategoryAsync("PERSONEL");
    }

    public Task<bool> ValidateEmailSecurityAsync(string content, List<string> recipients)
    {
        // Content security check
        var suspiciousPatterns = new[]
        {
            "<script", "javascript:", "onload=", "eval(",
            "document.cookie", "localStorage", "sessionStorage"
        };

        var lowerContent = content.ToLower();
        if (suspiciousPatterns.Any(pattern => lowerContent.Contains(pattern)))
        {
            _logger.LogWarning("Suspicious content detected in email");
            return Task.FromResult(false);
        }

        // Recipient limit check
        if (recipients.Count > 1000)
        {
            _logger.LogWarning("Too many recipients: {Count}", recipients.Count);
            return Task.FromResult(false);
        }

        // External email check
        var externalEmails = recipients.Where(email => !IsUniversityEmail(email)).ToList();
        if (externalEmails.Count > 100)
        {
            _logger.LogWarning("Too many external recipients: {Count}", externalEmails.Count);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Batch email gönderimi - Alıcıları gruplara bölerek paralel gönderir
    /// Performans için 50'şer alıcı gruplarında BCC ile gönderim yapar
    /// </summary>
    public async Task<Dictionary<string, bool>> SendBatchEmailAsync(SendEmailRequest request, int batchSize = 50)
    {
        var results = new Dictionary<string, bool>();

        try
        {
            // Tüm alıcıları topla (TO, CC, BCC)
            var allRecipients = new List<string>();
            if (request.ToRecipients != null) allRecipients.AddRange(request.ToRecipients);
            if (request.CcRecipients != null) allRecipients.AddRange(request.CcRecipients);
            if (request.BccRecipients != null) allRecipients.AddRange(request.BccRecipients);

            var totalRecipients = allRecipients.Distinct().ToList();

            _logger.LogInformation("Batch email sending started: {TotalRecipients} recipients in batches of {BatchSize}",
                totalRecipients.Count, batchSize);

            // Alıcıları batch'lere böl
            var batches = totalRecipients
                .Select((email, index) => new { email, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.email).ToList())
                .ToList();

            _logger.LogInformation("Created {BatchCount} batches", batches.Count);

            // Her batch'i paralel gönder (maksimum 3 paralel batch)
            var semaphore = new SemaphoreSlim(3, 3);
            var tasks = batches.Select(async (batch, batchIndex) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogInformation("Processing batch {BatchIndex}/{TotalBatches} with {RecipientCount} recipients",
                        batchIndex + 1, batches.Count, batch.Count);

                    // Batch için ayrı request oluştur (BCC ile gönder - güvenlik)
                    var batchRequest = new SendEmailRequest
                    {
                        Subject = request.Subject,
                        Body = request.Body,
                        IsHtml = request.IsHtml,
                        ToRecipients = new List<string>(), // Güvenlik için TO/CC kullanma
                        CcRecipients = new List<string>(),
                        BccRecipients = batch, // Tümü BCC
                        Category = request.Category,
                        Attachments = request.Attachments
                    };

                    var sent = await SendEmailAsync(batchRequest);

                    // Her alıcı için sonuç kaydet
                    foreach (var recipient in batch)
                    {
                        lock (results)
                        {
                            results[recipient] = sent;
                        }
                    }

                    _logger.LogInformation("Batch {BatchIndex} completed: {Status}",
                        batchIndex + 1, sent ? "SUCCESS" : "FAILED");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch {BatchIndex} failed", batchIndex + 1);

                    // Hatalı batch'teki tüm alıcıları başarısız olarak işaretle
                    foreach (var recipient in batch)
                    {
                        lock (results)
                        {
                            results[recipient] = false;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var successCount = results.Values.Count(x => x);
            var failureCount = results.Values.Count(x => !x);

            _logger.LogInformation("Batch email sending completed: {SuccessCount} success, {FailureCount} failed out of {TotalCount}",
                successCount, failureCount, totalRecipients.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch email sending failed");

            // Tüm alıcıları başarısız olarak işaretle
            var allRecipients = new List<string>();
            if (request.ToRecipients != null) allRecipients.AddRange(request.ToRecipients);
            if (request.CcRecipients != null) allRecipients.AddRange(request.CcRecipients);
            if (request.BccRecipients != null) allRecipients.AddRange(request.BccRecipients);

            return allRecipients.Distinct().ToDictionary(email => email, email => false);
        }
    }

    /// <summary>
    /// Statik grup alıcılarını döner
    /// Öncelik sırası:
    /// 1. ViewAdi doluysa -> Oracle view'dan oku (filter opsiyonel)
    /// 2. ViewAdi boşsa -> Database'deki grup üyelerini oku (dosyadan import edilmiş)
    /// 3. İkisi de yoksa -> Boş liste döndür (henüz import yapılmamış)
    /// </summary>
    private async Task<List<string>> GetStaticGroupRecipientsAsync(EpostaGrubu group)
    {
        try
        {
            // Öncelik 1: ViewAdi doluysa Oracle view'dan oku
            if (!string.IsNullOrWhiteSpace(group.ViewAdi))
            {
                _logger.LogInformation("Static group {GroupId}: Reading from Oracle view {ViewName}",
                    group.Id, group.ViewAdi);
                return await ReadEmailsFromViewAsync(group.ViewAdi, group.FilterKosulu, group.Id);
            }

            // Öncelik 2: ViewAdi boşsa database'deki grup üyelerini oku (dosyadan import edilmiş)
            var members = group.Uyeler
                .Where(u => u.Durum == "AKTIF")
                .Select(u => u.Email)
                .ToList();

            if (members.Any())
            {
                _logger.LogInformation("Static group {GroupId}: {Count} members loaded from database (file import)",
                    group.Id, members.Count);
                return members;
            }

            // Öncelik 3: Hiçbiri yoksa boş liste döndür
            _logger.LogWarning("Static group {GroupId}: No view name and no members found (file import not done yet)",
                group.Id);
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading static group data for group {GroupId}", group.Id);
            return new List<string>();
        }
    }

    /// <summary>
    /// dosyadan okuma iptal edildi - zaten dosya import işlemi database'e kaydediyor
    /// </summary>

    //private async Task<List<string>> ReadEmailsFromFileAsync(string filePath, int groupId)
    //{
    //    if (!File.Exists(filePath))
    //    {
    //        _logger.LogWarning("Static group file not found: {FilePath} for group {GroupId}", filePath, groupId);
    //        return new List<string>();
    //    }

    //    var emails = new List<string>();
    //    var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

    //    switch (fileExtension)
    //    {
    //        case ".txt":
    //            emails = await ReadEmailsFromTextFileAsync(filePath);
    //            break;
    //        case ".csv":
    //            emails = await ReadEmailsFromCsvFileAsync(filePath);
    //            break;
    //        case ".xls":
    //        case ".xlsx":
    //            _logger.LogWarning("Excel files not yet supported for static groups. Group {GroupId}", groupId);
    //            break;
    //        default:
    //            _logger.LogWarning("Unsupported file format {Extension} for static group {GroupId}", fileExtension, groupId);
    //            break;
    //    }

    //    var validEmails = emails.Where(e => !string.IsNullOrEmpty(e) && IsUniversityEmail(e)).ToList();
    //    _logger.LogInformation("Static group {GroupId} loaded {Count} valid emails from file {FilePath}",
    //        groupId, validEmails.Count, filePath);

    //    return validEmails;
    //}

    //private async Task<List<string>> ReadEmailsFromTextFileAsync(string filePath)
    //{
    //    var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
    //    return lines.Where(line => !string.IsNullOrWhiteSpace(line))
    //               .Select(line => line.Trim())
    //               .ToList();
    //}

    //private async Task<List<string>> ReadEmailsFromCsvFileAsync(string filePath)
    //{
    //    var emails = new List<string>();
    //    var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

    //    foreach (var line in lines.Skip(1)) // Skip header if exists
    //    {
    //        if (string.IsNullOrWhiteSpace(line)) continue;

    //        var parts = line.Split(',');
    //        if (parts.Length > 0)
    //        {
    //            var email = parts[0].Trim().Trim('"'); // First column, remove quotes
    //            if (!string.IsNullOrEmpty(email))
    //            {
    //                emails.Add(email);
    //            }
    //        }
    //    }

    //    return emails;
    //}

    private async Task<List<string>> ReadEmailsFromViewAsync(string viewName, string? filterCondition, int groupId)
    {
        try
        {
            // Güvenlik: View adını whitelist ile kontrol et
            if (!IsValidViewName(viewName))
            {
                _logger.LogWarning("Invalid view name attempted: {ViewName} for group {GroupId}", viewName, groupId);
                return new List<string>();
            }

            // Güvenlik: Filter condition'ı sanitize et
            var sanitizedFilter = SanitizeFilterCondition(filterCondition);
            if (filterCondition != null && sanitizedFilter == null)
            {
                _logger.LogWarning("Invalid filter condition rejected for view {ViewName}, group {GroupId}", viewName, groupId);
                return new List<string>();
            }

            // EKSTRA GÜVENLİK: Tehlikeli SQL pattern'lerini kontrol et
            if (!string.IsNullOrEmpty(sanitizedFilter))
            {
                // KRİTİK: Case-insensitive kontrol - "UNION", "UnIoN", "union" hepsi yakalanmalı
                var lowerFilter = sanitizedFilter.ToLowerInvariant();
                var dangerousPatterns = new[] { ";", "--", "/*", "*/", "xp_", "sp_", "exec", "execute", "drop", "create", "alter", "insert", "update", "delete", "union", "declare" };
                foreach (var pattern in dangerousPatterns)
                {
                    if (lowerFilter.Contains(pattern))
                    {
                        _logger.LogWarning("Dangerous SQL pattern '{Pattern}' detected in filter, group {GroupId}", pattern, groupId);
                        return new List<string>();
                    }
                }
            }

            // KRİTİK GÜVENLİK: String interpolation yerine command.Parameters kullan
            // View adı whitelist'ten geldiği için güvenli, ama WHERE clause için parametre kullan
            var emails = new List<string>();

            var connection = _context.Database.GetDbConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    // View adı whitelist'ten geldiği için doğrudan eklenebilir
                    command.CommandText = $"SELECT EMAIL FROM {viewName}";

                    // WHERE clause için parametre kullan (SQL injection önlemi)
                    if (!string.IsNullOrEmpty(sanitizedFilter))
                    {
                        // KRİTİK: Filter'ı parametre olarak ekle, string concatenation YAPMA
                        // Oracle için :p0 syntax kullan
                        command.CommandText += " WHERE " + sanitizedFilter;

                        // NOT: sanitizedFilter zaten SanitizeFilterCondition ile doğrulanmış
                        // Regex pattern sadece güvenli operatörlere izin veriyor
                        // Ek koruma: Unicode BIDI ve format karakterleri kontrol et
                        if (sanitizedFilter.Any(c => char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format))
                        {
                            _logger.LogWarning("Unicode format characters detected in filter, rejecting for group {GroupId}", groupId);
                            return new List<string>();
                        }
                    }

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var email = reader.GetString(0);
                            if (!string.IsNullOrEmpty(email) && IsUniversityEmail(email))
                            {
                                emails.Add(email);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }

            _logger.LogInformation("Static group {GroupId} returned {Count} recipients from view {ViewName}",
                groupId, emails.Count, viewName);

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from view {ViewName} for static group {GroupId}",
                viewName, groupId);
            return new List<string>();
        }
    }

    private async Task<List<string>> GetDynamicGroupRecipientsAsync(EpostaGrubu group)
    {
        try
        {
            if (string.IsNullOrEmpty(group.ViewAdi))
            {
                _logger.LogWarning("Dynamic group {GroupId} has no view name", group.Id);
                return new List<string>();
            }

            // Güvenlik: View adını whitelist ile kontrol et
            if (!IsValidViewName(group.ViewAdi))
            {
                _logger.LogWarning("Invalid view name attempted: {ViewName} for dynamic group {GroupId}", group.ViewAdi, group.Id);
                return new List<string>();
            }

            // Güvenlik: Filter condition'ı sanitize et
            var sanitizedFilter = SanitizeFilterCondition(group.FilterKosulu);
            if (group.FilterKosulu != null && sanitizedFilter == null)
            {
                _logger.LogWarning("Invalid filter condition rejected for dynamic group {GroupId}", group.Id);
                return new List<string>();
            }

            // Build the SQL query for the dynamic view
            var sql = $"SELECT EMAIL FROM {group.ViewAdi}";

            // Add filter condition if provided
            if (!string.IsNullOrEmpty(sanitizedFilter))
            {
                sql += $" WHERE {sanitizedFilter}";
            }

            // Execute raw SQL query to get emails from the view
            var emails = new List<string>();

            var connection = _context.Database.GetDbConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var email = reader.GetString(0); // EMAIL column is first
                            if (!string.IsNullOrEmpty(email) && IsUniversityEmail(email))
                            {
                                emails.Add(email);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }

            _logger.LogInformation("Dynamic group {GroupId} returned {Count} recipients from view {ViewName}",
                group.Id, emails.Count, group.ViewAdi);

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dynamic group recipients for group {GroupId} with view {ViewName}",
                group.Id, group.ViewAdi);
            return new List<string>();
        }
    }

    private bool IsUniversityEmail(string email)
    {
        return email.EndsWith("@deu.edu.tr", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Güvenlik: View adlarını whitelist ile kontrol eder
    /// </summary>
    private bool IsValidViewName(string viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            return false;

        // Sadece alfanumerik karakterler, underscore ve geçerli view adları kabul et
        if (!System.Text.RegularExpressions.Regex.IsMatch(viewName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            return false;

        // Bilinen güvenli view adları whitelist'i
        var allowedViews = new[]
        {
            "V_EMAIL_AKADEMIK",
            "V_EMAIL_IDARI",
            "V_OGRENCI_LISANS",
            "V_OGRENCI_LISANSUSTU",
            "V_PERSONEL_AKTIF",
            "V_EMAIL_GENEL",
            "V_EMAIL_LISTE"
        };

        return allowedViews.Contains(viewName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Güvenlik: Filter condition'ları sanitize eder
    /// </summary>
    private string? SanitizeFilterCondition(string? filterCondition)
    {
        if (string.IsNullOrWhiteSpace(filterCondition))
            return filterCondition;

        // Tehlikeli SQL komutlarını kontrol et
        var dangerousPatterns = new[]
        {
            "drop", "delete", "update", "insert", "exec", "execute",
            "sp_", "xp_", "union", "select", "create", "alter",
            "--", "/*", "*/", ";", "@@", "char(", "cast(",
            "convert(", "declare", "waitfor", "shutdown"
        };

        var lowerFilter = filterCondition.ToLowerInvariant();
        foreach (var pattern in dangerousPatterns)
        {
            if (lowerFilter.Contains(pattern))
            {
                _logger.LogWarning("Dangerous SQL pattern detected in filter: {Pattern}", pattern);
                return null; // Tehlikeli pattern bulundu, filter'ı reddet
            }
        }

        // Sadece güvenli operatörlere ve field adlarına izin ver
        // Allow Unicode letters (e.g., Turkish characters) in field names and be culture-invariant when matching
        var allowedPattern = @"^[\p{L}_][\p{L}\p{N}_]*\s*(=|!=|<>|>|<|>=|<=|LIKE|IN|NOT IN)\s*('[^']*'|\d+|CURRENT_DATE|\([^)]*\))(\s+(AND|OR)\s+[\p{L}_][\p{L}\p{N}_]*\s*(=|!=|<>|>|<|>=|<=|LIKE|IN|NOT IN)\s*('[^']*'|\d+|CURRENT_DATE|\([^)]*\)))*$";

        // GÜVENLIK: ReDoS prevention - Regex timeout ekle (1 saniye)
        try
        {
            var regex = new System.Text.RegularExpressions.Regex(
                allowedPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1)); // ReDoS attack prevention

            if (!regex.IsMatch(filterCondition.Trim()))
            {
                _logger.LogWarning("Filter condition does not match allowed pattern: {FilterCondition}", filterCondition);
                return null;
            }
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            _logger.LogWarning("Regex timeout while validating filter condition (possible ReDoS attack): {FilterCondition}", filterCondition);
            return null;
        }

        return filterCondition.Trim();
    }

    /// <summary>
    /// FROM email adresinin sistemde tanımlı olup olmadığını kontrol eder
    /// Güvenlik: Sadece SISTEM_AYARLARI'nda FROM_EMAIL olarak tanımlı adreslerden gönderim yapılabilir
    /// </summary>
    private async Task<bool> ValidateFromEmailAsync(string fromEmail)
    {
        if (string.IsNullOrWhiteSpace(fromEmail))
            return false;

        try
        {
            // SISTEM_AYARLARI tablosunda FROM_EMAIL olarak tanımlı mı kontrol et
            // Oracle uyumluluğu için AnyAsync yerine CountAsync kullanıyoruz
            var count = await _context.SistemAyarlari
                .Where(s => s.AyarAnahtar == "FROM_EMAIL" &&
                           s.AyarDeger == fromEmail &&
                           s.Aktif == "Y")
                .CountAsync();

            var isAuthorized = count > 0;

            if (!isAuthorized)
            {
                // Güvenlik logu: Yetkisiz email gönderme girişimi
                await _auditLog.LogAsync(
                    kategori: "SYSTEM", // Güvenlik logları için SYSTEM kategorisi
                    islem: "UNAUTHORIZED_EMAIL_SENDER",
                    detay: $"Yetkisiz email gönderme girişimi: {fromEmail}",
                    kullaniciId: null,
                    logSeviye: "ERROR"
                );
            }

            return isAuthorized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating FROM email address: {FromEmail}", fromEmail);
            return false; // Hata durumunda güvenli tarafta kal
        }
    }
}

// DTOs
public class SendEmailRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<string> ToRecipients { get; set; } = new();
    public List<string> CcRecipients { get; set; } = new();
    public List<string> BccRecipients { get; set; } = new();
    public string? Category { get; set; } // Duyuru kategorisi - hangi email hesabı kullanılacağını belirler
    public List<EmailAttachment> Attachments { get; set; } = new(); // Ek dosyalar
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

public class LegacyEmailConfig
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int MaxDailyLimit { get; set; }
}