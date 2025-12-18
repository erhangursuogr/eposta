using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.Enums;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DeuEposta.Services;

public interface ISecurityService
{
    Task<bool> ValidateLoginAttemptAsync(string email, string ip, string userAgent);

    Task<bool> IsCaptchaRequiredAsync(string email, string ip);

    Task LogSecurityEventAsync(string eventType, string description, string ip, int? userId = null);

    Task<bool> IsContentSafeAsync(string content);

    Task<bool> IsEmailContentSafeAsync(string subject, string content);

    Task<SecurityValidationResult> ValidateAnnouncementAsync(int userId, string content, List<string> recipients);

    Task<string> GenerateSecureTokenAsync();

    bool IsValidEmail(string email);

    bool IsUniversityEmail(string email);

    void RecordFailedLoginAttempt(string email, string ip, string userAgent);

    void RecordSuccessfulLogin(string email, string ip);

    /// <summary>
    /// HTML içeriğini XSS saldırılarına karşı sanitize eder.
    /// Script, iframe, object, embed gibi tehlikeli elementleri ve event handler'ları temizler.
    /// Email içeriği için güvenli HTML döndürür.
    /// </summary>
    string SanitizeHtmlContent(string htmlContent);

    /// <summary>
    /// Email içeriğini hem kontrol eder hem de sanitize eder.
    /// Tehlikeli pattern tespit edilirse null döner, değilse sanitize edilmiş içerik döner.
    /// </summary>
    Task<(bool IsSafe, string? SanitizedContent)> ValidateAndSanitizeEmailContentAsync(string content);
}

public class SecurityValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public SecurityRiskLevel RiskLevel { get; set; }
}

public enum SecurityRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class SecurityService : ISecurityService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<SecurityService> _logger;
    private readonly IConfiguration _configuration;

    // In-memory cache for performance (production'da Redis kullanılabilir)
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, LoginAttemptInfo> _loginAttempts = new();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _captchaRequiredIPs = new();

    public SecurityService(DeuEpostaContext context, ILogger<SecurityService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> ValidateLoginAttemptAsync(string email, string ip, string userAgent)
    {
        var isDevelopment = _configuration.GetValue<string>("Environment") == "Development";

        await LogSecurityEventAsync("LOGIN_ATTEMPT",
            $"Login attempt for Email: {email}, IP: {ip}, UserAgent: {userAgent}", ip);

        // Development modunda engelleme yapmayalım, ama production'da güvenlik kontrollerini uygula
        if (isDevelopment)
        {
            return true;
        }

        var key = $"{ip}:{email}";

        // Brute force koruması
        if (_loginAttempts.ContainsKey(key))
        {
            var attemptInfo = _loginAttempts[key];

            // GÜVENLIK: 15 dakika içinde 3'ten fazla başarısız deneme varsa engelle
            // Rapor önerisi: 3 attempts / 15 min (5→3, 5min→15min)
            if (attemptInfo.FailedCount >= 3 &&
                DateTime.UtcNow.Subtract(attemptInfo.LastAttempt).TotalMinutes < 15)
            {
                await LogSecurityEventAsync("BRUTE_FORCE_BLOCKED",
                    $"Brute force attempt blocked for {email} from {ip}", ip);
                return false;
            }
        }

        // Şüpheli User-Agent kontrolü
        if (IsSuspiciousUserAgent(userAgent))
        {
            await LogSecurityEventAsync("SUSPICIOUS_USER_AGENT",
                $"Suspicious user agent: {userAgent} for {email}", ip);
            return false;
        }

        return true;
    }

    private bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return true;

        var suspiciousPatterns = new[]
        {
            "sqlmap", "nikto", "nmap", "burp", "owasp", "hack", "exploit",
            "bot", "crawler", "spider", "scan"
        };

        return suspiciousPatterns.Any(pattern =>
            userAgent.ToLower().Contains(pattern));
    }

    public Task<bool> IsCaptchaRequiredAsync(string email, string ip)
    {
        var key = $"{ip}:{email}";

        // 3 başarısız denemeden sonra CAPTCHA gerekli
        if (_loginAttempts.ContainsKey(key) && _loginAttempts[key].FailedCount >= 3)
        {
            _captchaRequiredIPs[ip] = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        // Şüpheli IP'ler için her zaman CAPTCHA
        if (_captchaRequiredIPs.ContainsKey(ip))
        {
            var captchaTime = _captchaRequiredIPs[ip];
            if (DateTime.UtcNow.Subtract(captchaTime).TotalHours < 24)
            {
                return Task.FromResult(true);
            }
            _captchaRequiredIPs.TryRemove(ip, out _);
        }

        return Task.FromResult(false);
    }

    public async Task<bool> IsContentSafeAsync(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;

        var dangerousPatterns = new[]
        {
            // Script injection
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
            @"javascript\s*:",
            @"on\w+\s*=",

            // HTML injection
            @"<iframe\b",
            @"<object\b",
            @"<embed\b",
            @"<link\b",

            // SQL patterns (unlikely in email content but check anyway)
            @"\bunion\s+select\b",
            @"\bdrop\s+table\b",

            // Email header injection
            @"\r\n|\n|\r",
            @"(to|cc|bcc|from|subject)\s*:",

            // Phishing indicators
            @"(click\s+here|urgent|suspended|verify\s+account)",
            @"(bank|paypal|amazon|apple|microsoft).*login",

            // Malicious URLs (basic check)
            @"https?://(?:[0-9]{1,3}\.){3}[0-9]{1,3}", // IP-based URLs
            @"bit\.ly|tinyurl|shorturl", // URL shorteners
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                await LogSecurityEventAsync("DANGEROUS_CONTENT",
                    $"Dangerous pattern detected: {pattern}", "System");
                return false;
            }
        }

        return true;
    }

    public async Task<bool> IsEmailContentSafeAsync(string subject, string content)
    {
        // Subject kontrolü
        if (!await IsContentSafeAsync(subject))
            return false;

        // Content kontrolü
        if (!await IsContentSafeAsync(content))
            return false;

        // Spam indicators
        var spamIndicators = new[]
        {
            "free", "win", "prize", "urgent", "act now", "limited time",
            "click here", "unsubscribe", "viagra", "lottery", "casino"
        };

        var combinedText = $"{subject} {content}".ToLower();
        var spamScore = spamIndicators.Count(indicator => combinedText.Contains(indicator));

        if (spamScore >= 3)
        {
            await LogSecurityEventAsync("POTENTIAL_SPAM",
                $"High spam score ({spamScore}): {subject}", "System");
            return false;
        }

        return true;
    }

    public async Task<SecurityValidationResult> ValidateAnnouncementAsync(int userId, string content, List<string> recipients)
    {
        var result = new SecurityValidationResult { IsValid = true, RiskLevel = SecurityRiskLevel.Low };

        // Kullanıcı geçmişini kontrol et
        var user = await _context.Kullanicilar
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            result.IsValid = false;
            result.Errors.Add("Kullanıcı bulunamadı");
            result.RiskLevel = SecurityRiskLevel.Critical;
            return result;
        }

        // Yeni kullanıcı kontrolü (30 gün içinde oluşturulan)
        if (user.OlusturmaTarihi > DateTime.UtcNow.AddDays(-30))
        {
            result.Warnings.Add("Yeni kullanıcı - ekstra dikkat gerekli");
            result.RiskLevel = SecurityRiskLevel.Medium;
        }

        // İçerik güvenlik kontrolü
        if (!await IsEmailContentSafeAsync("", content))
        {
            result.IsValid = false;
            result.Errors.Add("Güvenli olmayan içerik tespit edildi");
            result.RiskLevel = SecurityRiskLevel.High;
        }

        // Alıcı sayısı kontrolü (spam koruması)
        if (recipients.Count > 1000 && user.Rol?.RolKodu != RolKodu.ADMIN)
        {
            result.IsValid = false;
            result.Errors.Add("Çok fazla alıcı - ADMIN onayı gerekli");
            result.RiskLevel = SecurityRiskLevel.High;
        }

        // Harici email kontrolü (sadece DEU domainleri kabul et)
        var externalEmails = recipients.Where(email => !IsUniversityEmail(email)).ToList();
        if (externalEmails.Any() && user.Rol?.RolKodu is not (RolKodu.ADMIN or RolKodu.MANAGER))
        {
            result.Warnings.Add($"Harici email adresleri tespit edildi: {externalEmails.Count}");
            result.RiskLevel = SecurityRiskLevel.Medium;
        }

        // Günlük duyuru sayısı kontrolü (UTC safe)
        var utcToday = DateTime.UtcNow.Date;
        var utcTomorrow = utcToday.AddDays(1);
        var todayAnnouncements = await _context.EpostaDuyurulari
            .CountAsync(d => d.OlusturanKullaniciId == userId &&
                           d.OlusturmaTarihi >= utcToday && d.OlusturmaTarihi < utcTomorrow);

        if (todayAnnouncements >= 5 && user.Rol?.RolKodu is not RolKodu.ADMIN)
        {
            result.IsValid = false;
            result.Errors.Add("Günlük duyuru limitini aştınız (5 adet)");
            result.RiskLevel = SecurityRiskLevel.Medium;
        }

        return result;
    }

    public Task<string> GenerateSecureTokenAsync()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Task.FromResult(Convert.ToBase64String(bytes));
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public bool IsUniversityEmail(string email)
    {
        if (!IsValidEmail(email)) return false;

        var allowedDomains = _configuration.GetSection("Security:AllowedEmailDomains")
            .Get<string[]>() ?? new[] { "deu.edu.tr", "deu.edu.tr" };

        var domain = email.Split('@').LastOrDefault()?.ToLower();
        return allowedDomains.Contains(domain);
    }

    public async Task LogSecurityEventAsync(string eventType, string description, string ip, int? userId = null)
    {
        try
        {
            var logEntry = new LogSistem
            {
                Kategori = "SYSTEM",
                Islem = eventType,
                Detay = description,
                IpAdres = ip,
                KullaniciId = userId,
                LogTarihi = DateTime.Now,
                LogSeviye = GetLogLevel(eventType)
            };

            _context.LogSistem.Add(logEntry);
            await _context.SaveChangesAsync();

            // Standard logging
            _logger.LogWarning("Security Event: {EventType} - {Description}. IP: {IpAddress}, User: {UserId}, Severity: {Severity}",
                eventType, description, ip, userId, GetLogLevel(eventType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
        }
    }

    private void CleanupOldAttempts()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        // ConcurrentDictionary'de iterasyon sırasında silme güvenli değil
        // Önce silinecek anahtarları topla, sonra sil
        var expiredKeys = _loginAttempts
            .Where(kvp => kvp.Value.LastAttempt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _loginAttempts.TryRemove(key, out _);
        }
    }

    private string GetLogLevel(string eventType)
    {
        return eventType switch
        {
            "BRUTE_FORCE_BLOCKED" => "ERROR",
            "DANGEROUS_CONTENT" => "ERROR",
            "SUSPICIOUS_LOGIN" => "WARNING",
            "RAPID_LOGIN_ATTEMPTS" => "WARNING",
            _ => "INFO"
        };
    }

    public void RecordFailedLoginAttempt(string email, string ip, string userAgent)
    {
        var key = $"{ip}:{email}";
        var now = DateTime.UtcNow;

        CleanupOldAttempts();

        _loginAttempts.AddOrUpdate(key,
            new LoginAttemptInfo
            {
                FailedCount = 1,
                LastAttempt = now,
                UserAgent = userAgent
            },
            (existingKey, existingAttempts) =>
            {
                existingAttempts.FailedCount++;
                existingAttempts.LastAttempt = now;
                existingAttempts.UserAgent = userAgent;
                return existingAttempts;
            });
    }

    public void RecordSuccessfulLogin(string email, string ip)
    {
        var key = $"{ip}:{email}";

        // Remove the failed attempts record on successful login
        _loginAttempts.TryRemove(key, out _);
    }

    /// <summary>
    /// HTML içeriğini XSS saldırılarına karşı sanitize eder.
    /// Email için güvenli HTML tagları ve attribute'ları korur.
    /// </summary>
    public string SanitizeHtmlContent(string htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return htmlContent;

        try
        {
            var sanitizer = CreateEmailSanitizer();
            var sanitizedHtml = sanitizer.Sanitize(htmlContent);

            _logger.LogDebug("HTML content sanitized. Original length: {OriginalLength}, Sanitized length: {SanitizedLength}",
                htmlContent.Length, sanitizedHtml.Length);

            return sanitizedHtml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML content, returning empty string for safety");
            return string.Empty;
        }
    }

    /// <summary>
    /// Email içeriğini kontrol eder ve sanitize eder.
    /// Tehlikeli pattern varsa (false, null), güvenliyse (true, sanitizedContent) döner.
    /// </summary>
    public async Task<(bool IsSafe, string? SanitizedContent)> ValidateAndSanitizeEmailContentAsync(string content)
    {
        if (string.IsNullOrEmpty(content))
            return (true, content);

        // Önce tehlikeli patternleri kontrol et
        if (!await IsContentSafeAsync(content))
        {
            _logger.LogWarning("Email content failed safety check, rejecting");
            return (false, null);
        }

        // Sanitize et
        var sanitizedContent = SanitizeHtmlContent(content);

        // Sanitize sonrası tekrar kontrol (paranoid mode)
        if (!await IsContentSafeAsync(sanitizedContent))
        {
            _logger.LogWarning("Sanitized content still contains dangerous patterns, rejecting");
            return (false, null);
        }

        return (true, sanitizedContent);
    }

    /// <summary>
    /// Email içeriği için özelleştirilmiş HtmlSanitizer oluşturur.
    /// Güvenli HTML tagları ve attribute'ları izin verir.
    /// </summary>
    private static HtmlSanitizer CreateEmailSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // Varsayılan ayarları temizle ve email için güvenli olanları ekle
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedCssProperties.Clear();

        // Email için güvenli HTML tagları
        var allowedTags = new[]
        {
            // Yapısal
            "div", "span", "p", "br", "hr",
            // Başlıklar
            "h1", "h2", "h3", "h4", "h5", "h6",
            // Listeler
            "ul", "ol", "li",
            // Tablo
            "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
            // Metin formatlama
            "b", "strong", "i", "em", "u", "s", "strike", "del", "ins", "sub", "sup",
            "blockquote", "pre", "code", "q", "cite", "abbr", "address",
            // Link ve resim
            "a", "img",
            // Diğer
            "figure", "figcaption", "center", "font"
        };

        foreach (var tag in allowedTags)
        {
            sanitizer.AllowedTags.Add(tag);
        }

        // Güvenli attribute'lar
        var allowedAttributes = new[]
        {
            "class", "id", "style",
            "href", "target", "title", "rel",  // Link
            "src", "alt", "width", "height",   // Image
            "align", "valign", "bgcolor", "border", "cellpadding", "cellspacing",  // Table
            "colspan", "rowspan", "scope",
            "color", "size", "face"  // Font (legacy)
        };

        foreach (var attr in allowedAttributes)
        {
            sanitizer.AllowedAttributes.Add(attr);
        }

        // Güvenli CSS özellikleri
        var allowedCss = new[]
        {
            "color", "background-color", "background",
            "font-family", "font-size", "font-weight", "font-style",
            "text-align", "text-decoration", "text-indent", "line-height",
            "margin", "margin-top", "margin-bottom", "margin-left", "margin-right",
            "padding", "padding-top", "padding-bottom", "padding-left", "padding-right",
            "border", "border-color", "border-width", "border-style",
            "border-top", "border-bottom", "border-left", "border-right",
            "border-collapse", "border-spacing",
            "width", "height", "max-width", "max-height", "min-width", "min-height",
            "display", "vertical-align", "float", "clear",
            "list-style", "list-style-type"
        };

        foreach (var css in allowedCss)
        {
            sanitizer.AllowedCssProperties.Add(css);
        }

        // URL scheme'leri (sadece güvenli protokoller)
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

        // Link'ler için target="_blank" zorunlu rel="noopener noreferrer" ekle
        sanitizer.PostProcessNode += (sender, args) =>
        {
            if (args.Node is HtmlAgilityPack.HtmlNode htmlNode && htmlNode.Name == "a")
            {
                var target = htmlNode.GetAttributeValue("target", "");
                if (target == "_blank")
                {
                    htmlNode.SetAttributeValue("rel", "noopener noreferrer");
                }
            }
        };

        return sanitizer;
    }

    private class LoginAttemptInfo
    {
        public int FailedCount { get; set; }
        public DateTime LastAttempt { get; set; }
        public string UserAgent { get; set; } = string.Empty;
    }
}