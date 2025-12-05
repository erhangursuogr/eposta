using DeuEposta.Data;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Jobs;

/// <summary>
/// Orphan (duyuruya bağlanmamış) dosyaları temizleyen scheduled job.
///
/// Çalışma Mantığı:
/// 1. SESSION_ID dolu ama DUYURU_ID boş olan dosyaları bul (7+ gün eski)
/// 2. Fiziksel dosyayı diskten sil
/// 3. DB kaydını sil (hard delete)
///
/// Schedule: Her Pazar gece 03:00 (Cron.Weekly(DayOfWeek.Sunday, 3))
/// Retention: 7 gün (kullanıcı 1 hafta içinde duyuruyu kaydetmezse orphan temizlenir)
/// </summary>
public class OrphanFileCleanupJob
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<OrphanFileCleanupJob> _logger;
    private readonly IConfiguration _configuration;

    public OrphanFileCleanupJob(
        DeuEpostaContext context,
        ILogger<OrphanFileCleanupJob> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// 7 günden eski, duyuruya bağlanmamış dosyaları temizler (hard delete)
    /// </summary>
    public async Task CleanupOrphanFilesAsync()
    {
        var startTime = DateTime.Now;
        _logger.LogInformation("=== Orphan File Cleanup Job Started ===");

        try
        {
            // Upload root path'i al (FileService ile aynı mantık)
            var uploadPathFromDb = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == "DOSYA" && s.AyarAnahtar == "DEPOLAMA_KLASORU" && s.Aktif == "Y")
                .Select(s => s.AyarDeger)
                .FirstOrDefaultAsync();

            var uploadPath = !string.IsNullOrEmpty(uploadPathFromDb)
                ? uploadPathFromDb
                : Path.Combine(Directory.GetCurrentDirectory(), "uploads");

            _logger.LogInformation("Upload path: {UploadPath}", uploadPath);

            // 7 günden eski orphan dosyaları bul
            var cutoffDate = DateTime.Now.AddDays(-7);
            var orphanFiles = await _context.Dosyalar
                .Where(f => f.SessionId != null && f.DuyuruId == null && f.YuklemeTarihi < cutoffDate)
                .ToListAsync();

            if (orphanFiles.Count == 0)
            {
                _logger.LogInformation("No orphan files found older than 7 days.");
                return;
            }

            _logger.LogInformation("Found {Count} orphan files to cleanup", orphanFiles.Count);

            int deletedPhysicalFiles = 0;
            int deletedDbRecords = 0;
            int failedDeletions = 0;
            long totalFreedBytes = 0;

            foreach (var file in orphanFiles)
            {
                try
                {
                    // Fiziksel dosyayı sil
                    var fullPath = Path.Combine(uploadPath, file.DosyaYolu);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        deletedPhysicalFiles++;
                        totalFreedBytes += file.DosyaBoyutu;
                        _logger.LogDebug("Deleted physical file: {Path}", fullPath);
                    }
                    else
                    {
                        _logger.LogWarning("Physical file not found (already deleted?): {Path}", fullPath);
                    }

                    // DB kaydını sil (hard delete)
                    _context.Dosyalar.Remove(file);
                    deletedDbRecords++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete orphan file {FileId} ({FileName})", file.Id, file.DosyaAdi);
                    failedDeletions++;
                }
            }

            // Değişiklikleri kaydet
            await _context.SaveChangesAsync();

            var duration = DateTime.Now - startTime;
            var freedMB = totalFreedBytes / (1024.0 * 1024.0);

            _logger.LogInformation(
                "=== Orphan File Cleanup Completed === " +
                "Duration: {Duration:c}, " +
                "Physical files deleted: {PhysicalDeleted}, " +
                "DB records deleted: {DbDeleted}, " +
                "Failed: {Failed}, " +
                "Disk space freed: {FreedMB:F2} MB",
                duration, deletedPhysicalFiles, deletedDbRecords, failedDeletions, freedMB);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orphan file cleanup job failed");
            throw; // Hangfire retry mechanism
        }
    }
}