namespace DeuEposta.Utils;

/// <summary>
/// Pagination parametrelerinin güvenli validasyonu için yardımcı sınıf
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Maksimum sayfa boyutu - OOM saldırılarını önler
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Varsayılan sayfa boyutu
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Pagination parametrelerini güvenli değerlere normalize eder
    /// </summary>
    /// <param name="page">Sayfa numarası (1-based)</param>
    /// <param name="pageSize">Sayfa boyutu</param>
    /// <returns>Normalize edilmiş (page, pageSize) tuple</returns>
    public static (int page, int pageSize) Normalize(int page, int pageSize)
    {
        // page < 1 ise 1 yap (negatif Skip değerini önle)
        page = Math.Max(1, page);

        // pageSize'ı 1-MaxPageSize aralığına sınırla (OOM saldırısını önle)
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        return (page, pageSize);
    }

    /// <summary>
    /// Varsayılan değerlerle pagination parametrelerini normalize eder
    /// </summary>
    public static (int page, int pageSize) NormalizeWithDefaults(int? page, int? pageSize)
    {
        return Normalize(page ?? 1, pageSize ?? DefaultPageSize);
    }
}
