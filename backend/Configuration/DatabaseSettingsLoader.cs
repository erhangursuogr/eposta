using DeuEposta.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DeuEposta.Configuration;

/// <summary>
/// Veritabanından uygulama ayarlarını yükler
/// </summary>
public static class DatabaseSettingsLoader
{
    /// <summary>
    /// MAX_DOSYA_BOYUTU_MB ayarını veritabanından okur
    /// </summary>
    public static long GetMaxFileSizeBytes(string connectionString)
    {
        const long defaultSizeBytes = 50 * 1024 * 1024; // 50 MB

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<DeuEpostaContext>();
            optionsBuilder.UseOracle(connectionString);

            using var context = new DeuEpostaContext(optionsBuilder.Options);

            var maxFileSizeSetting = context.SistemAyarlari
                .FirstOrDefault(s => s.AyarKategori == "DOSYA"
                    && s.AyarAnahtar == "MAX_DOSYA_BOYUTU_MB"
                    && s.Aktif == "Y");

            if (maxFileSizeSetting != null && int.TryParse(maxFileSizeSetting.AyarDeger, out int maxFileSizeMB))
            {
                var sizeBytes = maxFileSizeMB * 1024L * 1024L;                
                return sizeBytes;
            }
            return defaultSizeBytes;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read MAX_DOSYA_BOYUTU_MB from database, using default 50 MB");
            return defaultSizeBytes;
        }
    }
}