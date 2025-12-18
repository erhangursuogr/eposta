namespace DeuEposta.Models.DTOs;

/// <summary>
/// SMS gönderim sonucu
/// </summary>
public class SmsResult
{
    /// <summary>
    /// SMS başarıyla gönderildi mi?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Sonuç mesajı (başarılı veya hata mesajı)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Oracle'dan dönen detaylı sonuç (opsiyonel)
    /// </summary>
    public string? OracleResult { get; set; }
}
