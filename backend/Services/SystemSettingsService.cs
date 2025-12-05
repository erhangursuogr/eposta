using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface ISystemSettingsService
{
    Task<ResponseDataModel<List<SystemEmailSetting>>> GetEmailSettingsAsync();
    Task<ResponseModel> UpdateEmailSettingsAsync(UpdateEmailSettingsRequest request);
    Task<ResponseDataModel<List<SystemSetting>>> GetAllSettingsAsync(string? category = null, bool includeSecret = false, bool includeInactive = false);
    Task<ResponseDataModel<List<ManagerUserDto>>> GetManagersAsync();

    // Generic setting getters - Tüm servisler için kullanılabilir
    Task<string?> GetSettingValueAsync(string category, string key);
    Task<T> GetSettingValueAsync<T>(string category, string key, T defaultValue);

    // Email kategori ve imza yönetimi
    Task<List<EmailCategoryDto>> GetEmailCategoriesAsync(string? rolKodu = null, int? kullaniciGorevYeri = null);
    Task<string> GetEmailSignatureAsync(string? category);

    // SMTP test
    Task<ResponseDataModel<SmtpTestResult>> TestSmtpConnectionAsync(string category, string? testEmailAddress = null);

    // CRUD operations
    Task<ResponseDataModel<SystemSetting>> CreateSettingAsync(CreateSystemSettingRequest request);
    Task<ResponseModel> UpdateSettingAsync(int id, UpdateSystemSettingRequest request);
    Task<ResponseModel> DeleteSettingAsync(int id);
    Task<ResponseModel> BulkUpdateSettingsAsync(List<BulkUpdateSettingRequest> requests);
}

public class SystemSettingsService : ISystemSettingsService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<SystemSettingsService> _logger;
    private readonly IAuditLogService _auditLog;

    public SystemSettingsService(
        DeuEpostaContext context,
        ILogger<SystemSettingsService> logger,
        IAuditLogService auditLog)
    {
        _context = context;
        _logger = logger;
        _auditLog = auditLog;
    }

    public async Task<ResponseDataModel<List<SystemEmailSetting>>> GetEmailSettingsAsync()
    {
        try
        {
            var settingsData = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == "EMAIL" && s.Aktif == "Y")
                .ToListAsync();

            var settings = settingsData.Select(s => new SystemEmailSetting
                {
                    Key = s.AyarAnahtar ?? string.Empty,
                    Value = s.AyarDeger ?? string.Empty,
                    Description = s.AyarAciklama ?? string.Empty,
                    IsSecret = s.Gizli == "Y"
                })
                .ToList();

            return ResponseDataModel<List<SystemEmailSetting>>.SuccessResult(settings, "Email ayarları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email settings");
            return ResponseDataModel<List<SystemEmailSetting>>.ErrorResult("Email ayarları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> UpdateEmailSettingsAsync(UpdateEmailSettingsRequest request)
    {
        try
        {
            var changedSettings = new List<string>();

            // N+1 problemi önlendi: Tüm ayarları tek sorguda al
            var keys = request.Settings.Select(s => s.Key).ToList();
            var existingSettings = await _context.SistemAyarlari
                .Where(s => keys.Contains(s.AyarAnahtar))
                .ToDictionaryAsync(s => s.AyarAnahtar!);

            foreach (var setting in request.Settings)
            {
                if (existingSettings.TryGetValue(setting.Key, out var existingSetting))
                {
                    var oldValue = existingSetting.Gizli == "Y" ? "***" : existingSetting.AyarDeger;
                    var newValue = existingSetting.Gizli == "Y" ? "***" : setting.Value;

                    if (existingSetting.AyarDeger != setting.Value)
                    {
                        changedSettings.Add($"{existingSetting.AyarKategori}.{setting.Key}: {oldValue} → {newValue}");
                    }

                    existingSetting.AyarDeger = setting.Value;
                    existingSetting.GuncellemeTarihi = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("System settings updated: {Count} changes", changedSettings.Count);

            // Audit log: Sistem ayarları değişikliği
            if (changedSettings.Any())
            {
                await _auditLog.LogAsync(
                    kategori: "SYSTEM",
                    islem: "SISTEM_AYAR_DEGISIKLIK",
                    detay: $"Sistem ayarları güncellendi. Değişiklikler: {string.Join(", ", changedSettings)}",
                    logSeviye: "WARN" // Sistem ayarı değişikliği kritik
                );
            }

            return ResponseModel.SuccessResult($"Sistem ayarları güncellendi ({changedSettings.Count} değişiklik)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return ResponseModel.ErrorResult("Sistem ayarları güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<SystemSetting>>> GetAllSettingsAsync(string? category = null, bool includeSecret = false, bool includeInactive = false)
    {
        try
        {
            var query = _context.SistemAyarlari.AsQueryable();

            // Aktif/Pasif filtresi
            if (!includeInactive)
            {
                query = query.Where(s => s.Aktif == "Y");
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.AyarKategori == category);
            }

            if (!includeSecret)
            {
                query = query.Where(s => s.Gizli != "Y");
            }

            var settingsData = await query.ToListAsync();

            var settings = settingsData.Select(s => new SystemSetting
                {
                    Id = s.Id,
                    Category = s.AyarKategori ?? string.Empty,
                    Key = s.AyarAnahtar ?? string.Empty,
                    Value = s.AyarDeger ?? string.Empty,
                    Description = s.AyarAciklama ?? string.Empty,
                    Gizli = s.Gizli ?? "N",
                    Aktif = s.Aktif ?? "Y",
                    GorevYeri = s.GorevYeri
                })
                .ToList();

            return ResponseDataModel<List<SystemSetting>>.SuccessResult(settings, "Sistem ayarları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system settings");
            return ResponseDataModel<List<SystemSetting>>.ErrorResult("Sistem ayarları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ManagerUserDto>>> GetManagersAsync()
    {
        try
        {
            var managers = await _context.Kullanicilar
                .Include(k => k.Rol)
                .Where(k => k.Rol != null && k.Rol.RolKodu == "MANAGER" && k.Aktif == "Y")
                .Select(k => new ManagerUserDto
                {
                    Id = k.Id,
                    AdSoyad = k.AdSoyad ?? string.Empty,
                    Email = k.Email ?? string.Empty
                })
                .OrderBy(k => k.AdSoyad)
                .ToListAsync();

            return ResponseDataModel<List<ManagerUserDto>>.SuccessResult(managers, "Yöneticiler alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting managers");
            return ResponseDataModel<List<ManagerUserDto>>.ErrorResult("Yöneticiler alınırken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Belirtilen kategori ve anahtara sahip ayarın değerini getirir
    /// </summary>
    public async Task<string?> GetSettingValueAsync(string category, string key)
    {
        try
        {
            var setting = await _context.SistemAyarlari
                .FirstOrDefaultAsync(s => s.AyarKategori == category &&
                                         s.AyarAnahtar == key &&
                                         s.Aktif == "Y");

            return setting?.AyarDeger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting value for {Category}.{Key}", category, key);
            return null;
        }
    }

    /// <summary>
    /// Belirtilen kategori ve anahtara sahip ayarın değerini getirir (type-safe)
    /// </summary>
    public async Task<T> GetSettingValueAsync<T>(string category, string key, T defaultValue)
    {
        try
        {
            var value = await GetSettingValueAsync(category, key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            // Type conversion
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);
            if (typeof(T) == typeof(bool))
                return (T)(object)(value == "Y" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting typed setting value for {Category}.{Key}", category, key);
            return defaultValue;
        }
    }

    /// <summary>
    /// Tüm aktif email imza kategorilerini getirir (dropdown için)
    /// GOREV_YERI filtrelemesi:
    /// - ADMIN rolü: Tüm imzaları görebilir
    /// - Diğer roller (EDITOR, COORDINATOR, MANAGER, VIEWER): Sadece kendi görev yeri + ortak imzalar (NULL)
    /// - GOREV_YERI = 0 (Rektörlük/Merkez): Tüm imzalar
    /// </summary>
    public async Task<List<EmailCategoryDto>> GetEmailCategoriesAsync(string? rolKodu = null, int? kullaniciGorevYeri = null)
    {
        try
        {
            // EMAIL_IMZA kategorisinden imza kategorilerini al
            var query = _context.SistemAyarlari
                .Where(s => s.AyarKategori == "EMAIL_IMZA" && s.Aktif == "Y");

            // GOREV_YERI filtrelemesi - ADMIN hariç tüm roller kısıtlı
            var isAdmin = rolKodu == "ADMIN";
            //var isMerkezPersoneli = kullaniciGorevYeri.HasValue && kullaniciGorevYeri.Value == 0;

            if (!isAdmin && kullaniciGorevYeri.HasValue)
            {
                // ADMIN hariç: Sadece kendi görev yeri + ortak imzalar (NULL)
                query = query.Where(s => s.GorevYeri == null || s.GorevYeri == kullaniciGorevYeri.Value);
                _logger.LogInformation("Filtering signatures for {Role} with GorevYeri={GorevYeri}", rolKodu, kullaniciGorevYeri);
            }
            else
            {
                // ADMIN veya Merkez personeli (GOREV_YERI=0): Tüm imzaları görebilir
                _logger.LogInformation("No signature filtering applied (Role={Role}, GorevYeri={GorevYeri}, IsAdmin={IsAdmin})",
                    rolKodu, kullaniciGorevYeri, isAdmin);
            }

            var settingsData = await query.ToListAsync();

            var categories = settingsData.Select(s => new EmailCategoryDto
                {
                    Key = s.AyarAnahtar ?? string.Empty,
                    DisplayName = s.AyarAciklama ?? s.AyarAnahtar ?? string.Empty,
                    HasSignature = !string.IsNullOrWhiteSpace(s.AyarDeger)
                })
                .OrderBy(c => c.DisplayName)
                .ToList();

            _logger.LogInformation("Loaded {Count} email signature categories", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email signature categories");
            return new List<EmailCategoryDto>();
        }
    }

    /// <summary>
    /// Kategori için imza HTML kodunu getirir
    /// NULL/boş kategori = imza yok (empty string dön)
    /// </summary>
    public async Task<string> GetEmailSignatureAsync(string? category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
                return string.Empty; // İmza yok

            var signature = await GetSettingValueAsync("EMAIL_IMZA", category);
            return signature ?? string.Empty; // Kategori bulunamadıysa da imza yok
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email signature for category {Category}", category);
            return string.Empty;
        }
    }

    /// <summary>
    /// SMTP bağlantı testi yapar
    /// </summary>
    public async Task<ResponseDataModel<SmtpTestResult>> TestSmtpConnectionAsync(string category, string? testEmailAddress = null)
    {
        try
        {
            // Kategori zaten "EMAIL_" ile başlıyorsa olduğu gibi kullan, yoksa ekle
            var categoryKey = category.StartsWith("EMAIL_") ? category : $"EMAIL_{category}";

            // Kategori-specific ayarları al
            var categorySettings = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == categoryKey && s.Aktif == "Y")
                .ToDictionaryAsync(s => s.AyarAnahtar!, s => s.AyarDeger);

            // Ortak SMTP ayarlarını al
            var commonSettings = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == "EMAIL_ORTAK" && s.Aktif == "Y")
                .ToDictionaryAsync(s => s.AyarAnahtar!, s => s.AyarDeger);

            var fromEmail = categorySettings.GetValueOrDefault("FROM_EMAIL", "duyuru@deu.edu.tr") ?? "duyuru@deu.edu.tr";
            var fromName = categorySettings.GetValueOrDefault("FROM_NAME", "DEÜ Duyuru Sistemi") ?? "DEÜ Duyuru Sistemi";
            var smtpServer = commonSettings.GetValueOrDefault("SMTP_SERVER", "giden.posta.deu.edu.tr") ?? "giden.posta.deu.edu.tr";
            var smtpPort = int.Parse(commonSettings.GetValueOrDefault("SMTP_PORT", "25") ?? "25");
            var enableSsl = (commonSettings.GetValueOrDefault("ENABLE_SSL", "N") ?? "N") == "Y";
            var smtpUsername = categorySettings.GetValueOrDefault("SMTP_USERNAME", "") ?? "";
            var smtpPassword = categorySettings.GetValueOrDefault("SMTP_PASSWORD", "") ?? "";
            var useDefaultCredentials = (commonSettings.GetValueOrDefault("USE_DEFAULT_CREDENTIALS", "Y") ?? "Y") == "Y";

            // Test email adresini belirle: parametreden gelirse onu kullan, yoksa FROM_EMAIL
            var recipientEmail = !string.IsNullOrEmpty(testEmailAddress) ? testEmailAddress : fromEmail;

            // Test email gönder
            using var client = new System.Net.Mail.SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.UseDefaultCredentials = useDefaultCredentials;

            if (!useDefaultCredentials && !string.IsNullOrEmpty(smtpUsername))
            {
                client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
            }

            // Bağlantı testi için timeout kısa tut
            client.Timeout = 10000; // 10 saniye

            // Test mesajı oluştur
            var testMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail, fromName),
                Subject = "SMTP Bağlantı Testi - DEÜ Duyuru Sistemi",
                Body = $"Bu bir SMTP bağlantı test mesajıdır.\n\nSMTP Sunucu: {smtpServer}:{smtpPort}\nSSL: {enableSsl}\nGönderen: {fromEmail}\n\nBu mesaj otomatik olarak oluşturulmuştur.",
                IsBodyHtml = false
            };
            testMessage.To.Add(recipientEmail);

            // SMTP sunucusuna bağlan ve gönder
            await client.SendMailAsync(testMessage);

            var result = new SmtpTestResult
            {
                Server = smtpServer,
                Port = smtpPort,
                Ssl = enableSsl,
                From = fromEmail
            };

            return ResponseDataModel<SmtpTestResult>.SuccessResult(
                result,
                $"SMTP bağlantısı başarılı! Test email {recipientEmail} adresine gönderildi."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed for category {Category}", category);
            return ResponseDataModel<SmtpTestResult>.ErrorResult($"SMTP bağlantı hatası: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Yeni sistem ayarı oluştur
    /// </summary>
    public async Task<ResponseDataModel<SystemSetting>> CreateSettingAsync(CreateSystemSettingRequest request)
    {
        try
        {
            // Aynı kategori + key var mı kontrol et
            var existingCount = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == request.Category && s.AyarAnahtar == request.Key)
                .CountAsync();

            if (existingCount > 0)
                return ResponseDataModel<SystemSetting>.ErrorResult("Bu kategori ve anahtar kombinasyonu zaten mevcut", 400);

            var newSetting = new SistemAyar
            {
                AyarKategori = request.Category,
                AyarAnahtar = request.Key,
                AyarDeger = request.Value,
                AyarAciklama = request.Description,
                Gizli = request.IsSecret ? "Y" : "N",
                Aktif = request.IsActive ? "Y" : "N",
                GorevYeri = request.GorevYeri,
                OlusturmaTarihi = DateTime.Now,
                GuncellemeTarihi = DateTime.Now
            };

            _context.SistemAyarlari.Add(newSetting);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                kategori: "SYSTEM",
                islem: "SISTEM_AYAR_OLUSTURMA",
                detay: $"Yeni ayar oluşturuldu: {request.Category}.{request.Key}",
                logSeviye: "INFO"
            );

            var result = new SystemSetting
            {
                Id = newSetting.Id,
                Category = newSetting.AyarKategori ?? string.Empty,
                Key = newSetting.AyarAnahtar ?? string.Empty,
                Value = newSetting.AyarDeger ?? string.Empty,
                Description = newSetting.AyarAciklama ?? string.Empty,
                Gizli = newSetting.Gizli ?? "N",
                Aktif = newSetting.Aktif ?? "Y",
                GorevYeri = newSetting.GorevYeri
            };

            return ResponseDataModel<SystemSetting>.SuccessResult(result, "Ayar başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system setting");
            return ResponseDataModel<SystemSetting>.ErrorResult("Ayar oluşturulurken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Sistem ayarını güncelle
    /// </summary>
    public async Task<ResponseModel> UpdateSettingAsync(int id, UpdateSystemSettingRequest request)
    {
        try
        {
            var setting = await _context.SistemAyarlari.FindAsync(id);
            if (setting == null)
                return ResponseModel.ErrorResult("Ayar bulunamadı", 404);

            var oldValue = setting.Gizli == "Y" ? "***" : setting.AyarDeger;
            var newValue = setting.Gizli == "Y" ? "***" : request.Value;

            setting.AyarDeger = request.Value;
            if (request.Description != null)
                setting.AyarAciklama = request.Description;
            if (request.IsActive.HasValue)
                setting.Aktif = request.IsActive.Value ? "Y" : "N";
            if (request.GorevYeri.HasValue)
                setting.GorevYeri = request.GorevYeri.Value;
            setting.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                kategori: "SYSTEM",
                islem: "SISTEM_AYAR_GUNCELLEME",
                detay: $"Ayar güncellendi: {setting.AyarKategori}.{setting.AyarAnahtar}: {oldValue} → {newValue}",
                logSeviye: "WARN"
            );

            return ResponseModel.SuccessResult("Ayar başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system setting {Id}", id);
            return ResponseModel.ErrorResult("Ayar güncellenirken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Sistem ayarını sil
    /// </summary>
    public async Task<ResponseModel> DeleteSettingAsync(int id)
    {
        try
        {
            var setting = await _context.SistemAyarlari.FindAsync(id);
            if (setting == null)
                return ResponseModel.ErrorResult("Ayar bulunamadı", 404);

            var category = setting.AyarKategori;
            var key = setting.AyarAnahtar;

            _context.SistemAyarlari.Remove(setting);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                kategori: "SYSTEM",
                islem: "SISTEM_AYAR_SILME",
                detay: $"Ayar silindi: {category}.{key}",
                logSeviye: "WARN"
            );

            return ResponseModel.SuccessResult("Ayar başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting system setting {Id}", id);
            return ResponseModel.ErrorResult("Ayar silinirken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Birden fazla ayarı toplu güncelle (SMTP grup yönetimi için)
    /// </summary>
    public async Task<ResponseModel> BulkUpdateSettingsAsync(List<BulkUpdateSettingRequest> requests)
    {
        try
        {
            if (requests == null || !requests.Any())
                return ResponseModel.ErrorResult("Güncellenecek ayar bulunamadı", 400);

            // N+1 problemi önlendi: Tüm ayarları tek sorguda al
            var ids = requests.Select(r => r.Id).ToList();
            var settings = await _context.SistemAyarlari
                .Where(s => ids.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            var updatedCount = 0;
            var changes = new List<string>();

            foreach (var request in requests)
            {
                if (!settings.TryGetValue(request.Id, out var setting))
                {
                    _logger.LogWarning("Setting with ID {Id} not found during bulk update", request.Id);
                    continue;
                }

                var oldValue = setting.Gizli == "Y" ? "***" : setting.AyarDeger;
                var newValue = setting.Gizli == "Y" ? "***" : request.Value;

                setting.AyarDeger = request.Value;
                if (request.Description != null)
                    setting.AyarAciklama = request.Description;
                if (request.IsActive.HasValue)
                    setting.Aktif = request.IsActive.Value ? "Y" : "N";
                setting.GuncellemeTarihi = DateTime.Now;

                changes.Add($"{setting.AyarKategori}.{setting.AyarAnahtar}: {oldValue} → {newValue}");
                updatedCount++;
            }

            if (updatedCount == 0)
                return ResponseModel.ErrorResult("Hiçbir ayar güncellenemedi", 400);

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                kategori: "SYSTEM",
                islem: "SISTEM_AYAR_TOPLU_GUNCELLEME",
                detay: $"Toplu güncelleme: {updatedCount} ayar güncellendi. Değişiklikler: {string.Join("; ", changes)}",
                logSeviye: "WARN"
            );

            return ResponseModel.SuccessResult($"{updatedCount} ayar başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update of system settings");
            return ResponseModel.ErrorResult("Toplu güncelleme sırasında hata oluştu", 500);
        }
    }
}

public class SmtpTestResult
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool Ssl { get; set; }
    public string From { get; set; } = string.Empty;
}