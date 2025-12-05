namespace DeuEposta.Models.Enums;

/// <summary>
/// Email grubu tipleri
/// </summary>
public enum GrupTipi
{
    /// <summary>
    /// Normal - Standart email kuralları (TO, CC, BCC kullanılabilir)
    /// </summary>
    NORMAL,

    /// <summary>
    /// Static - Dış kaynaktan seçilmiş liste (XLS, CSV, TXT). Sadece BCC
    /// </summary>
    STATIK,

    /// <summary>
    /// Dynamic - SQL View ile üyeler gelir. Sadece BCC
    /// </summary>
    DINAMIK,

    /// <summary>
    /// Debis - Mevcut sistem entegrasyonu, gizli listeci email. Sadece BCC
    /// </summary>
    DEBIS
}

/// <summary>
/// Grup tipi uzantı metodları
/// </summary>
public static class GrupTipiExtensions
{
    /// <summary>
    /// Bu grup tipi BCC-only mu?
    /// </summary>
    public static bool IsBccOnly(this GrupTipi grupTipi)
    {
        return grupTipi switch
        {
            GrupTipi.NORMAL => false,
            GrupTipi.STATIK => true,
            GrupTipi.DINAMIK => true,
            GrupTipi.DEBIS => true,
            _ => true
        };
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
            GrupTipi.NORMAL => "Standart email gönderim kuralları",
            GrupTipi.STATIK => "Dış kaynaktan yüklenen özel liste",
            GrupTipi.DINAMIK => "SQL View ile otomatik güncellenen liste",
            GrupTipi.DEBIS => "Mevcut Debis sistemi entegrasyonu",
            _ => "Bilinmeyen tip"
        };
    }

    /// <summary>
    /// String değeri enum'a güvenli dönüşüm (STATIC/STATIK uyumsuzluğunu çözer)
    /// </summary>
    public static GrupTipi ParseSafely(string value)
    {
        return value?.ToUpper() switch
        {
            "NORMAL" => GrupTipi.NORMAL,
            "STATIK" or "STATIC" => GrupTipi.STATIK,
            "DINAMIK" or "DYNAMIC" => GrupTipi.DINAMIK,
            "DEBIS" => GrupTipi.DEBIS,
            _ => GrupTipi.NORMAL // Default fallback
        };
    }
}