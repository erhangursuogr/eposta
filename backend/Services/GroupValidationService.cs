using DeuEposta.Models.Enums;

namespace DeuEposta.Services;

/// <summary>
/// Email grup validation işlemleri
/// BCC kısıtlamalarını kontrol eder
/// </summary>
public static class GroupValidationService
{
    /// <summary>
    /// Grup tipine göre alıcı kategorisinin geçerli olup olmadığını kontrol eder
    /// </summary>
    public static bool IsValidRecipientCategory(string grupTipi, string kategori)
    {
        var parsedTip = GrupTipiExtensions.ParseSafely(grupTipi);

        // BCC-only gruplar için sadece BCC kategorisine izin ver
        if (parsedTip.IsBccOnly())
        {
            return kategori?.ToUpperInvariant() == "BCC";
        }

        // Normal gruplar için tüm kategorilere izin ver
        return kategori?.ToUpperInvariant() is "TO" or "CC" or "BCC";
    }

    /// <summary>
    /// Grup tipi için uygun kategori önerir
    /// </summary>
    public static string GetRecommendedCategory(string grupTipi)
    {
        var parsedTip = GrupTipiExtensions.ParseSafely(grupTipi);
        return parsedTip.IsBccOnly() ? "BCC" : "TO";
    }

    /// <summary>
    /// Validation error mesajı üretir
    /// </summary>
    public static string GetValidationError(string grupTipi, string kategori)
    {
        var parsedTip = GrupTipiExtensions.ParseSafely(grupTipi);

        if (parsedTip.IsBccOnly() && kategori?.ToUpperInvariant() != "BCC")
        {
            return $"{grupTipi} tipindeki gruplar sadece BCC kategorisinde kullanılabilir. Güvenlik ve gizlilik gereksinimlerine uygun olarak bu grup türleri alıcıların email adreslerini gizleyecek şekilde çalışır.";
        }

        return string.Empty;
    }
}