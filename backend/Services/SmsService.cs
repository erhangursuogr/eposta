using DeuEposta.Models.DTOs;
using Oracle.ManagedDataAccess.Client;

namespace DeuEposta.Services;

/// <summary>
/// SMS bilgilendirme servisi (Oracle SMS fonksiyonu entegrasyonu)
/// Kullanıcılara bilgilendirme SMS'i gönderir
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Kullanıcıya bilgilendirme SMS'i gönderir
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası</param>
    /// <param name="message">SMS içeriği</param>
    /// <param name="userId">Kullanıcı ID (opsiyonel, loglama için)</param>
    /// <returns>Gönderim sonucu</returns>
    Task<SmsResult> SendSmsAsync(string phoneNumber, string message, int? userId = null);

    /// <summary>
    /// Telefon numarasını normalize eder (905XXXXXXXXX formatına çevirir)
    /// </summary>
    /// <param name="phoneNumber">Ham telefon numarası</param>
    /// <returns>Normalize edilmiş telefon numarası</returns>
    string NormalizePhoneNumber(string phoneNumber);
}

public class SmsService : ISmsService
{
    private readonly string _connectionString;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
    {
        // Oracle 19c connection string
        _connectionString = Environment.GetEnvironmentVariable("EPOSTA_ORACLE_CONNECTION")
                           ?? configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("Oracle connection string is required. Set via appsettings.json or EPOSTA_ORACLE_CONNECTION environment variable.");
        _logger = logger;
    }

    /// <summary>
    /// Telefon numarasını 905XXXXXXXXX formatına çevirir
    /// Örnekler: 5321234567 -> 905321234567, 05321234567 -> 905321234567
    /// </summary>
    public string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Telefon numarası boş olamaz", nameof(phoneNumber));

        // Boşlukları ve özel karakterleri temizle
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();

        // 0 ile başlıyorsa (05321234567) -> 905321234567
        if (cleaned.StartsWith("0"))
        {
            return "9" + cleaned;
        }
        // 90 ile başlıyorsa zaten doğru formatta
        else if (cleaned.StartsWith("90"))
        {
            return cleaned;
        }
        // Sadece numara (5321234567) -> 905321234567
        else
        {
            return "90" + cleaned;
        }
    }

    public async Task<SmsResult> SendSmsAsync(string phoneNumber, string message, int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Telefon numarası boş olamaz", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("SMS içeriği boş olamaz", nameof(message));

        try
        {
            // Telefon numarasını normalize et
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            // TODO: Oracle SMS fonksiyonu buraya entegre edilecek
            // Şimdilik placeholder log
            _logger.LogInformation("SMS send requested. Phone: {Phone}, UserId: {UserId}, Message length: {Length}",
                normalizedPhone, userId, message.Length);

            // PLACEHOLDER: Oracle fonksiyonu çağrısı buraya gelecek
            // Örnek: SELECT XXDEU.SMS_PACKAGE.SEND_SMS(:phone, :message) FROM DUAL

            // Geçici olarak başarılı dön (Oracle fonksiyonu entegre edilince gerçek sonuç dönülecek)
            _logger.LogWarning("SMS service is not yet integrated with Oracle function. Returning mock success.");

            return new SmsResult
            {
                Success = true,
                Message = "SMS servisi henüz Oracle fonksiyonuna entegre edilmedi (placeholder)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS. Phone: {Phone}, UserId: {UserId}", phoneNumber, userId);

            return new SmsResult
            {
                Success = false,
                Message = $"SMS gönderimi başarısız: {ex.Message}"
            };
        }
    }
}
