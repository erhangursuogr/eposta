using DeuEposta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DeuEposta.Services;

/// <summary>
/// Lightweight email configuration - sadece gerekli alanlar
/// </summary>
public class EmailConfig
{
    public string Category { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public bool EnableSsl { get; set; } = false;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool UseDefaultCredentials { get; set; } = true;
}

public interface IEmailCategoryService
{
    Task<List<string>> GetActiveEmailCategoriesAsync();

    Task<EmailConfig> GetEmailConfigByCategoryAsync(string category);

    Task<bool> IsValidCategoryAsync(string category);

    Task<string> GetCategoryDisplayNameAsync(string category);
}

public class EmailCategoryService : IEmailCategoryService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<EmailCategoryService> _logger;
    private readonly IMemoryCache _cache;
    private readonly ISystemSettingsService _systemSettingsService;

    public EmailCategoryService(
        DeuEpostaContext context,
        ILogger<EmailCategoryService> logger,
        IMemoryCache cache,
        ISystemSettingsService systemSettingsService)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _systemSettingsService = systemSettingsService;
    }

    /// <summary>
    /// Cache süresi ayarını database'den alır
    /// </summary>
    private async Task<int> GetCacheExpirationMinutesAsync()
    {
        try
        {
            return await _systemSettingsService.GetSettingValueAsync("CACHE", "EMAIL_CONFIG_MINUTES", 30);
        }
        catch
        {
            return 30; // Fallback
        }
    }

    /// <summary>
    /// Aktif SMTP gönderici kategorilerini getirir (EMAIL_PERSONEL, EMAIL_REKTORLUK, vb.)
    /// Frontend dropdown için kullanılır
    /// </summary>
    public async Task<List<string>> GetActiveEmailCategoriesAsync()
    {
        try
        {
            // AYAR_KATEGORI 'EMAIL_' ile başlayan ve 'EMAIL_ORTAK', 'EMAIL_IMZA' olmayan kategorileri getir
            var categories = await _context.SistemAyarlari
                .Where(s => s.Aktif == "Y" &&
                           s.AyarKategori.StartsWith("EMAIL_") &&
                           s.AyarKategori != "EMAIL_ORTAK" &&
                           s.AyarKategori != "EMAIL_IMZA" &&
                           s.AyarAnahtar == "FROM_EMAIL") // Her SMTP grubunun FROM_EMAIL kaydı var
                .Select(s => s.AyarKategori) // EMAIL_PERSONEL, EMAIL_REKTORLUK, vb.
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Found {Count} active SMTP sender categories", categories.Count);
            return categories ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active SMTP sender categories");
            return new List<string> { "EMAIL_PERSONEL" }; // Fallback
        }
    }

    /// <summary>
    /// Kategoriye göre email konfigürasyonu getirir - cache ile
    /// </summary>
    public async Task<EmailConfig> GetEmailConfigByCategoryAsync(string category)
    {
        var cacheKey = $"EmailConfig_{category}";

        // Cache'den dene
        if (_cache.TryGetValue(cacheKey, out EmailConfig? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        try
        {
            // Kategori zaten "EMAIL_" ile başlıyorsa olduğu gibi kullan, yoksa ekle
            var categoryKey = category.StartsWith("EMAIL_") ? category : $"EMAIL_{category}";

            // GÜVENLİK: Kategori geçerli mi kontrol et - geçersizse exception fırlat
            if (!await IsValidCategoryAsync(categoryKey))
            {
                _logger.LogError("SECURITY WARNING: Invalid email category requested: {Category}", categoryKey);
                throw new UnauthorizedAccessException($"Geçersiz email kategorisi: {categoryKey}. Bu kategori sistemde tanımlı değil.");
            }

            // Kategori-specific ayarları al
            var categorySettings = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == categoryKey && s.Aktif == "Y")
                .ToDictionaryAsync(s => s.AyarAnahtar, s => s.AyarDeger);

            // Ortak SMTP ayarlarını al
            var commonSettings = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == "EMAIL_ORTAK" && s.Aktif == "Y")
                .ToDictionaryAsync(s => s.AyarAnahtar, s => s.AyarDeger);

            var config = new EmailConfig
            {
                Category = category,
                FromEmail = categorySettings.GetValueOrDefault("FROM_EMAIL", "duyuru@deu.edu.tr") ?? "duyuru@deu.edu.tr",
                FromName = categorySettings.GetValueOrDefault("FROM_NAME", "DEÜ Duyuru Sistemi") ?? "DEÜ Duyuru Sistemi",
                SmtpServer = commonSettings.GetValueOrDefault("SMTP_SERVER", "giden.posta.deu.edu.tr") ?? "giden.posta.deu.edu.tr",
                SmtpPort = int.Parse(commonSettings.GetValueOrDefault("SMTP_PORT", "25") ?? "25"),
                EnableSsl = (commonSettings.GetValueOrDefault("ENABLE_SSL", "N") ?? "N") == "Y",
                SmtpUsername = categorySettings.GetValueOrDefault("SMTP_USERNAME", "") ?? "",
                SmtpPassword = categorySettings.GetValueOrDefault("SMTP_PASSWORD", "") ?? "",
                UseDefaultCredentials = (commonSettings.GetValueOrDefault("USE_DEFAULT_CREDENTIALS", "Y") ?? "Y") == "Y"
            };

            // Cache'e ekle (süre database'den okunuyor)
            var cacheMinutes = await GetCacheExpirationMinutesAsync();
            _cache.Set(cacheKey, config, TimeSpan.FromMinutes(cacheMinutes));

            return config;
        }
        catch (UnauthorizedAccessException)
        {
            // Güvenlik exception'ı aynen fırlat
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email config for category {Category}", category);
            throw new InvalidOperationException($"Email konfigürasyonu alınırken hata oluştu: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// SMTP gönderici kategorisinin geçerli olup olmadığını kontrol eder
    /// Örnek: "EMAIL_PERSONEL", "EMAIL_REKTORLUK"
    /// </summary>
    public async Task<bool> IsValidCategoryAsync(string category)
    {
        if (string.IsNullOrEmpty(category))
            return false;

        try
        {
            // SMTP kategorisinin FROM_EMAIL ayarı var mı kontrol et
            var setting = await _context.SistemAyarlari
                .FirstOrDefaultAsync(s => s.AyarKategori == category &&
                                         s.AyarAnahtar == "FROM_EMAIL" &&
                                         s.Aktif == "Y");

            return setting != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SMTP category {Category}", category);
            return category == "EMAIL_PERSONEL"; // Fallback
        }
    }

    /// <summary>
    /// SMTP kategorisinin görünen adını getirir
    /// Örnek: "EMAIL_PERSONEL" → "Personel duyuruları gönderici email adresi"
    /// </summary>
    public async Task<string> GetCategoryDisplayNameAsync(string category)
    {
        try
        {
            // FROM_EMAIL ayarının açıklamasını al (en anlamlı açıklama)
            var setting = await _context.SistemAyarlari
                .FirstOrDefaultAsync(s => s.AyarKategori == category &&
                                         s.AyarAnahtar == "FROM_EMAIL" &&
                                         s.Aktif == "Y");

            // "Personel duyuruları gönderici email adresi" → "Personel" gibi kısa isim çıkar
            var displayName = setting?.AyarAciklama ?? category;

            // "Personel duyuruları..." → "Personel" kısa adını çıkar
            if (displayName.Contains(" "))
            {
                displayName = displayName.Split(' ')[0]; // İlk kelime
            }

            return displayName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting display name for SMTP category {Category}", category);
            return category.Replace("EMAIL_", ""); // "EMAIL_PERSONEL" → "PERSONEL"
        }
    }
}