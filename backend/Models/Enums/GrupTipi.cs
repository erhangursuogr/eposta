namespace DeuEposta.Models.Enums;

/// <summary>
/// Email grubu tipleri (Tüm tipler BCC-only - KVKK/GDPR uyumu)
/// </summary>
public enum GrupTipi
{
    /// <summary>
    /// Manuel - Elle eklenen email listesi. Sadece BCC
    /// </summary>
    MANUEL,

    /// <summary>
    /// Dosya - Dış kaynaktan yüklenmiş liste (Excel, CSV, TXT). Sadece BCC
    /// </summary>
    DOSYA,

    /// <summary>
    /// Dinamik - SQL View ile otomatik güncellenen liste. Sadece BCC
    /// </summary>
    DINAMIK,

    /// <summary>
    /// Debis - Listserv sistemi entegrasyonu, gizli listeci email. Sadece BCC
    /// </summary>
    DEBIS
}

/// <summary>
/// Grup tipi uzantı metodları
/// </summary>
public static class GrupTipiExtensions
{
    /// <summary>
    /// Bu grup tipi BCC-only mu? (Artık TÜM tipler BCC-only)
    /// </summary>
    public static bool IsBccOnly(this GrupTipi grupTipi)
    {
        // Güvenlik için tüm grup tipleri BCC-only (KVKK/GDPR uyumu)
        return true;
    }

    /// <summary>
    /// Bu grup tipi gizli listeci email kullanır mı?
    /// </summary>
    public static bool UsesHiddenListEmail(this GrupTipi grupTipi)
    {
        return grupTipi == GrupTipi.DEBIS;
    }

    /// <summary>
    /// Grup tipi açıklaması
    /// </summary>
    public static string GetDescription(this GrupTipi grupTipi)
    {
        return grupTipi switch
        {
            GrupTipi.MANUEL => "Elle eklenen email listesi",
            GrupTipi.DOSYA => "Dosyadan yüklenen email listesi",
            GrupTipi.DINAMIK => "Veritabanından otomatik güncellenen liste",
            GrupTipi.DEBIS => "Listserv sistemi entegrasyonu",
            _ => "Bilinmeyen tip"
        };
    }

    /// <summary>
    /// String değeri enum'a güvenli dönüşüm (Backward compatibility + alias desteği)
    /// </summary>
    public static GrupTipi ParseSafely(string value)
    {
        return value?.ToUpper() switch
        {
            // Yeni değerler
            "MANUEL" => GrupTipi.MANUEL,
            "DOSYA" => GrupTipi.DOSYA,
            "DINAMIK" or "DYNAMIC" => GrupTipi.DINAMIK,
            "DEBIS" => GrupTipi.DEBIS,

            // Backward compatibility (eski değerler)
            "NORMAL" or "STANDART" => GrupTipi.MANUEL,
            "STATIK" or "STATIC" => GrupTipi.DOSYA,

            _ => GrupTipi.MANUEL // Default fallback
        };
    }
}