using AutoMapper;
using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace DeuEposta.Services;

public interface IFileService
{
    Task<ResponseDataModel<List<Dosya>>> GetFilesAsync(int page = 1, int pageSize = 20, string? searchTerm = null, string? fileType = null);

    Task<ResponseDataModel<FileDetailView>> GetFileByIdAsync(int id);

    Task<ResponseDataModel<FileUploadResult>> UploadFileAsync(IFormFile file, int kullaniciId, string? description = null, int? announcementId = null, string? sessionId = null);

    Task<ResponseDataModel<List<FileUploadResult>>> UploadMultipleFilesAsync(List<IFormFile> files, int kullaniciId, string? description = null);

    Task<ResponseModel> DeleteFileAsync(int id, int kullaniciId);

    Task<ResponseDataModel<FileDownloadResult>> DownloadFileAsync(int id);

    Task<ResponseDataModel<List<Dosya>>> GetAnnouncementFilesAsync(int announcementId);

    Task<ResponseModel> AttachFileToAnnouncementAsync(int fileId, int announcementId, int kullaniciId);

    Task<ResponseModel> DetachFileFromAnnouncementAsync(int fileId, int announcementId, int kullaniciId);

    Task<ResponseModel> ValidateFileAsync(IFormFile file);

    Task<ResponseDataModel<FileInfo>> GetFileInfoAsync(int id);

    Task<ResponseDataModel<List<Dosya>>> GetSessionFilesAsync(string sessionId);

    Task<ResponseModel> LinkSessionFilesToAnnouncementAsync(string sessionId, int announcementId, int kullaniciId);
}

public class FileService : IFileService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<FileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly FileSettings _fileSettings;
    private readonly IAuditLogService _auditLog;
    private readonly string _uploadPath;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public FileService(
        DeuEpostaContext context,
        ILogger<FileService> logger,
        IConfiguration configuration,
        IMapper mapper,
        IAuditLogService auditLog,
        IOptions<FileSettings> fileSettings)
    {
        _context = context;
        _logger = logger;
        _fileSettings = fileSettings.Value;
        _configuration = configuration;
        _mapper = mapper;
        _auditLog = auditLog;

        // Upload klasörünü SISTEM_AYARLARI tablosundan oku
        var uploadPathFromDb = _context.SistemAyarlari
            .Where(s => s.AyarKategori == "DOSYA" && s.AyarAnahtar == "DEPOLAMA_KLASORU" && s.Aktif == "Y")
            .Select(s => s.AyarDeger)
            .FirstOrDefault();

        _uploadPath = uploadPathFromDb?.TrimEnd('/') ??
                      _configuration.GetValue<string>("FileSettings:UploadPath") ??
                      "uploads";

        // Maksimum dosya boyutunu SISTEM_AYARLARI tablosundan oku
        var maxFileSizeFromDb = _context.SistemAyarlari
            .Where(s => s.AyarKategori == "DOSYA" && s.AyarAnahtar == "MAX_DOSYA_BOYUTU_MB" && s.Aktif == "Y")
            .Select(s => s.AyarDeger)
            .FirstOrDefault();

        var maxFileSizeMB = int.TryParse(maxFileSizeFromDb, out var dbSize) && dbSize > 0
            ? dbSize
            : _configuration.GetValue<int?>("FileSettings:MaxFileSizeMB") ?? 10;

        _maxFileSize = maxFileSizeMB * 1024 * 1024; // Convert MB to bytes

        // İzin verilen dosya tiplerini SISTEM_AYARLARI tablosundan oku
        var allowedExtensionsFromDb = _context.SistemAyarlari
            .Where(s => s.AyarKategori == "DOSYA" && s.AyarAnahtar == "IZIN_VERILEN_TIPLER" && s.Aktif == "Y")
            .Select(s => s.AyarDeger)
            .FirstOrDefault();

        // Veritabanında bulunamazsa appsettings.json'dan oku (fallback)
        var allowedExtensionsConfig = allowedExtensionsFromDb ??
            _configuration.GetValue<string>("FileSettings:AllowedExtensions") ??
            "jpg,jpeg,png,gif,pdf";

        _allowedExtensions = allowedExtensionsConfig
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToArray();

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    public async Task<ResponseDataModel<List<Dosya>>> GetFilesAsync(int page = 1, int pageSize = 20, string? searchTerm = null, string? fileType = null)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            var query = _context.Dosyalar.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.DosyaAdi.Contains(searchTerm) ||
                                        (f.Aciklama != null && f.Aciklama.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(fileType))
            {
                query = query.Where(f => f.DosyaTipi == fileType);
            }

            var totalCount = await query.CountAsync();

            var files = await query
                .OrderByDescending(f => f.YuklemeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ResponseDataModel<List<Dosya>>.SuccessResultWithPagination(
                files, totalCount, page, pageSize, "Dosyalar başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files");
            return ResponseDataModel<List<Dosya>>.ErrorResult("Dosyalar alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<FileDetailView>> GetFileByIdAsync(int id)
    {
        try
        {
            var file = await _context.Dosyalar
                .Include(f => f.YukleyenKullanici)
                .Include(f => f.Duyuru)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return ResponseDataModel<FileDetailView>.ErrorResult("Dosya bulunamadı", 404);

            var fileView = _mapper.Map<FileDetailView>(file);

            return ResponseDataModel<FileDetailView>.SuccessResult(fileView, "Dosya başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {Id}", id);
            return ResponseDataModel<FileDetailView>.ErrorResult("Dosya alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<FileUploadResult>> UploadFileAsync(IFormFile file, int kullaniciId, string? description = null, int? announcementId = null, string? sessionId = null)
    {
        try
        {
            // Validate file
            var validationResult = await ValidateFileAsync(file);
            if (!validationResult.Success)
                return ResponseDataModel<FileUploadResult>.ErrorResult(validationResult.Message, 400);

            // Either announcementId or sessionId must be provided
            if (!announcementId.HasValue && string.IsNullOrEmpty(sessionId))
                return ResponseDataModel<FileUploadResult>.ErrorResult("Duyuru ID veya Session ID gerekli", 400);

            // SECURITY: Session ID validation - backend tarafından generate edilmiş olmalı
            // Format: {userId}_{guid} - örn: 123_a1b2c3d4e5f6...
            if (!string.IsNullOrEmpty(sessionId))
            {
                var sessionParts = sessionId.Split('_');
                if (sessionParts.Length != 2 ||
                    !int.TryParse(sessionParts[0], out var sessionUserId) ||
                    sessionUserId != kullaniciId)
                {
                    _logger.LogWarning("SECURITY: Invalid session ID format or user mismatch. SessionId: {SessionId}, UserId: {UserId}",
                        sessionId, kullaniciId);
                    return ResponseDataModel<FileUploadResult>.ErrorResult("Geçersiz session ID", 403);
                }
            }

            // PERFORMANCE OPTIMIZATION: Calculate file hash ONCE (not twice)
            // Hash hesaplarken dosyayı tek seferde oku
            string fileHash;
            using (var stream = file.OpenReadStream())
            {
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(stream);
                fileHash = Convert.ToBase64String(hashBytes);
            }

            // Aynı duyuruya aynı dosya (hash) birden fazla eklenemesin
            if (announcementId.HasValue)
            {
                var existingCount = await _context.Dosyalar
                    .CountAsync(f => f.DuyuruId == announcementId.Value && f.DosyaHash == fileHash && f.Aktif == "Y");

                if (existingCount > 0)
                    return ResponseDataModel<FileUploadResult>.ErrorResult("Bu dosya zaten bu duyuruya eklenmiş", 400);
            }

            // Check if file with same hash already exists (Deduplication - Hybrid Approach)
            var existingFile = await _context.Dosyalar
                .Where(f => f.DosyaHash == fileHash && f.Aktif == "Y")
                .OrderByDescending(f => f.YuklemeTarihi)
                .FirstOrDefaultAsync();

            Dosya dosya = null!;
            string relativePath = string.Empty;
            bool shouldUploadNewFile = true;

            if (existingFile != null)
            {
                // Mevcut fiziksel dosyanın varlığını kontrol et
                var existingFullPath = Path.Combine(_uploadPath, existingFile.DosyaYolu);

                if (File.Exists(existingFullPath))
                {
                    // HYBRID: Aynı içeriğe sahip dosya var, fiziksel dosyayı kullan ama yeni DB kaydı oluştur
                    _logger.LogInformation("File with same hash exists, reusing physical file: {Path}", existingFile.DosyaYolu);

                    relativePath = existingFile.DosyaYolu; // Mevcut fiziksel dosyayı kullan

                    dosya = new Dosya
                    {
                        DosyaAdi = file.FileName, // Kullanıcının verdiği dosya adı
                        DosyaYolu = relativePath, // Mevcut fiziksel dosyayı referans et
                        DosyaTipi = file.ContentType,
                        DosyaBoyutu = file.Length,
                        DosyaHash = fileHash,
                        Aciklama = description,
                        YukleyenKullaniciId = kullaniciId,
                        YuklemeTarihi = DateTime.Now,
                        DuyuruId = announcementId, // Duyuruya ekle
                        Aktif = "Y"
                    };

                    _context.Dosyalar.Add(dosya);
                    await _context.SaveChangesAsync();

                    await _auditLog.LogAsync(
                           kategori: "FILE",
                           islem: "FILE_UPLOAD",
                           detay: $"{file.FileName} dosyası yüklendi, fiziksel dosya {existingFile.Id} kullanıldı"
                       );

                    _logger.LogInformation("File deduplication: New DB record {Id} created, reusing physical file from {ExistingId}",
                        dosya.Id, existingFile.Id);

                    shouldUploadNewFile = false;
                }
                else
                {
                    // Fiziksel dosya silinmiş, yeni dosya yükle
                    _logger.LogWarning("Existing file record found but physical file missing: {Path}, uploading new file", existingFile.DosyaYolu);
                }
            }

            if (shouldUploadNewFile)
            {
                // Yeni dosya yükle (fiziksel dosya + DB kaydı)
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

                // Create temp folder based on session or announcement
                string tempFolder;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Session upload: uploads/temp/user-{kullaniciId}/{sessionId}/
                    tempFolder = Path.Combine(_uploadPath, "temp", $"user-{kullaniciId}", sessionId);
                    relativePath = Path.Combine("temp", $"user-{kullaniciId}", sessionId, uniqueFileName);
                }
                else
                {
                    // Normal upload: uploads/temp/user-{kullaniciId}/
                    tempFolder = Path.Combine(_uploadPath, "temp", $"user-{kullaniciId}");
                    relativePath = Path.Combine("temp", $"user-{kullaniciId}", uniqueFileName);
                }

                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                var fullPath = Path.Combine(_uploadPath, relativePath);

                // PERFORMANCE OPTIMIZATION: Save file to disk with streaming (no memory buffer)
                // 8KB buffer kullanarak dosyayı chunk chunk kaydet
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Save to database
                dosya = new Dosya
                {
                    DosyaAdi = file.FileName,
                    DosyaYolu = relativePath, // Store relative path
                    DosyaTipi = file.ContentType,
                    DosyaBoyutu = file.Length,
                    DosyaHash = fileHash,
                    Aciklama = description,
                    YukleyenKullaniciId = kullaniciId,
                    YuklemeTarihi = DateTime.Now,
                    DuyuruId = announcementId, // Duyuruya ekle (null if session upload)
                    SessionId = sessionId, // Session ID ekle
                    Aktif = "Y"
                };

                _context.Dosyalar.Add(dosya);
                await _context.SaveChangesAsync();

                await _auditLog.LogAsync(
                           kategori: "FILE",
                           islem: "FILE_UPLOAD",
                           detay: $"{file.FileName} dosyası yüklendi, fiziksel dosya yeni oluşturuldu"
                       );

                _logger.LogInformation("New file uploaded to temp: {Id} - {Name} - {Path} by user {UserId} (Session: {SessionId})",
                    dosya.Id, dosya.DosyaAdi, relativePath, kullaniciId, sessionId ?? "N/A");
            }

            var result = new FileUploadResult
            {
                FileId = dosya.Id,
                FileName = dosya.DosyaAdi,
                FileSize = dosya.DosyaBoyutu,
                FileType = dosya.DosyaTipi,
                UploadDate = dosya.YuklemeTarihi
            };

            return ResponseDataModel<FileUploadResult>.SuccessResult(result, "Dosya başarıyla yüklendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return ResponseDataModel<FileUploadResult>.ErrorResult("Dosya yüklenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<FileUploadResult>>> UploadMultipleFilesAsync(List<IFormFile> files, int kullaniciId, string? description = null)
    {
        try
        {
            if (files == null || files.Count == 0)
                return ResponseDataModel<List<FileUploadResult>>.ErrorResult("En az bir dosya seçilmelidir", 400);

            // Not: Duyuruya ekleme sırasında MAX_DOSYA_SAYISI_DUYURU kontrolü yapılır

            // Paralel dosya yükleme (max 4 eşzamanlı upload)
            var semaphore = new SemaphoreSlim(4, 4);
            var uploadTasks = files.Select(async file =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await UploadFileAsync(file, kullaniciId, description);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var uploadResults = await Task.WhenAll(uploadTasks);

            var results = new List<FileUploadResult>();
            var errors = new List<string>();

            foreach (var uploadResult in uploadResults)
            {
                if (uploadResult.Success && uploadResult.Data != null)
                {
                    results.Add(uploadResult.Data);
                }
                else
                {
                    var fileName = uploadResult.Data?.FileName ?? "unknown";
                    errors.Add($"{fileName}: {uploadResult.Message}");
                }
            }

            if (errors.Any())
            {
                var errorMessage = $"{results.Count} dosya yüklendi, {errors.Count} dosya başarısız. Hatalar: {string.Join(", ", errors)}";
                return ResponseDataModel<List<FileUploadResult>>.SuccessResult(results, errorMessage);
            }

            await _auditLog.LogAsync(
                           kategori: "FILE",
                           islem: "FILE_UPLOAD",
                           detay: $"{results.Count} dosya yüklendi"
                       );

            _logger.LogInformation("Multiple files uploaded: {Count}", results.Count);


            return ResponseDataModel<List<FileUploadResult>>.SuccessResult(results, $"{results.Count} dosya başarıyla yüklendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return ResponseDataModel<List<FileUploadResult>>.ErrorResult("Dosyalar yüklenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteFileAsync(int id, int kullaniciId)
    {
        try
        {
            var file = await _context.Dosyalar.FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return ResponseModel.ErrorResult("Dosya bulunamadı", 404);

            // Check if user has permission to delete (owner or admin)
            var user = await _context.Kullanicilar
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == kullaniciId);

            if (file.YukleyenKullaniciId != kullaniciId && user?.Rol?.RolKodu != "ADMIN")
                return ResponseModel.ErrorResult("Bu dosyayı silme yetkiniz yok", 403);

            // Check if file is being used in announcements - Oracle compatible check
            var usageCount = await _context.EpostaDuyurulari
                .CountAsync(d => d.BannerDosyaId == id);

            if (usageCount > 0)
                return ResponseModel.ErrorResult("Bu dosya duyurularda kullanıldığı için silinemez", 400);

            // Delete physical file
            var filePath = Path.Combine(_uploadPath, file.DosyaYolu);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Delete from database
            _context.Dosyalar.Remove(file);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                           kategori: "FILE",
                           islem: "FILE_DELETE",
                           detay: $"{file} dosyası silindi"
                       );

            _logger.LogInformation("File deleted: {Id} - {Name} by user {UserId}", id, file.DosyaAdi, kullaniciId);

            return ResponseModel.SuccessResult("Dosya başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Id}", id);
            return ResponseModel.ErrorResult("Dosya silinirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<FileDownloadResult>> DownloadFileAsync(int id)
    {
        try
        {
            var file = await _context.Dosyalar.FirstOrDefaultAsync(f => f.Id == id && f.Aktif == "Y");

            if (file == null)
                return ResponseDataModel<FileDownloadResult>.ErrorResult("Dosya bulunamadı", 404);

            var filePath = Path.Combine(_uploadPath, file.DosyaYolu);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Physical file not found: {Path}", filePath);
                return ResponseDataModel<FileDownloadResult>.ErrorResult("Fiziksel dosya bulunamadı", 404);
            }

            // For large files, use streaming instead of loading entire file into memory
            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo.Length > 10 * 1024 * 1024) // 10MB threshold
            {
                // Update download count first
                file.IndirmeSayisi = (file.IndirmeSayisi ?? 0) + 1;
                file.SonIndirmeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();

                // Return file stream for large files
                var result = new FileDownloadResult
                {
                    FileName = file.DosyaAdi,
                    ContentType = string.IsNullOrWhiteSpace(file.DosyaTipi) ? "application/octet-stream" : file.DosyaTipi,
                    FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read),
                    FileSize = file.DosyaBoyutu
                };
                return ResponseDataModel<FileDownloadResult>.SuccessResult(result, "Dosya başarıyla hazırlandı");
            }
            else
            {
                // Small files can be loaded into memory
                var fileBytes = await File.ReadAllBytesAsync(filePath);

                // Update download count
                file.IndirmeSayisi = (file.IndirmeSayisi ?? 0) + 1;
                file.SonIndirmeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();

                var result = new FileDownloadResult
                {
                    FileName = file.DosyaAdi,
                    ContentType = string.IsNullOrWhiteSpace(file.DosyaTipi) ? "application/octet-stream" : file.DosyaTipi,
                    FileContent = fileBytes,
                    FileSize = file.DosyaBoyutu
                };
                return ResponseDataModel<FileDownloadResult>.SuccessResult(result, "Dosya başarıyla indirildi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {Id}", id);
            return ResponseDataModel<FileDownloadResult>.ErrorResult("Dosya indirilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<Dosya>>> GetAnnouncementFilesAsync(int announcementId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);
            if (announcement == null)
                return ResponseDataModel<List<Dosya>>.ErrorResult("Duyuru bulunamadı", 404);

            var files = await _context.Dosyalar
                .Where(f => f.DuyuruId == announcementId)
                .Include(f => f.YukleyenKullanici)
                .Include(f => f.Duyuru)
                .ToListAsync();

            return ResponseDataModel<List<Dosya>>.SuccessResult(files, "Duyuru dosyaları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement files for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<List<Dosya>>.ErrorResult("Duyuru dosyaları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> AttachFileToAnnouncementAsync(int fileId, int announcementId, int kullaniciId)
    {
        try
        {
            var file = await _context.Dosyalar.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null)
                return ResponseModel.ErrorResult("Dosya bulunamadı", 404);

            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.EkDosyalar)
                .FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // Check max file limit per announcement (from SISTEM_AYARLARI)
            var maxFileCountStr = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == "DOSYA" && s.AyarAnahtar == "MAX_DOSYA_SAYISI_DUYURU" && s.Aktif == "Y")
                .Select(s => s.AyarDeger)
                .FirstOrDefaultAsync();

            var maxFilesPerAnnouncement = int.TryParse(maxFileCountStr, out var count) ? count : 5; // Varsayılan 5

            if (announcement.EkDosyalar.Count >= maxFilesPerAnnouncement)
                return ResponseModel.ErrorResult($"Bir duyuruya en fazla {maxFilesPerAnnouncement} dosya eklenebilir", 400);

            // Check if already attached
            if (announcement.EkDosyalar.Any(f => f.Id == fileId))
                return ResponseModel.ErrorResult("Dosya zaten duyuruya eklenmiş", 400);

            // Move file from temp to announcement folder
            var oldPath = Path.Combine(_uploadPath, file.DosyaYolu);

            if (File.Exists(oldPath))
            {
                // Create announcement folder: announcements/YYYY/MM/announcement-{id}/
                var year = announcement.OlusturmaTarihi.Year;
                var month = announcement.OlusturmaTarihi.Month.ToString("00");
                var announcementFolder = Path.Combine(_uploadPath, "announcements", year.ToString(), month, $"announcement-{announcementId}");

                if (!Directory.Exists(announcementFolder))
                    Directory.CreateDirectory(announcementFolder);

                var fileName = Path.GetFileName(file.DosyaYolu);
                var newRelativePath = Path.Combine("announcements", year.ToString(), month, $"announcement-{announcementId}", fileName);
                var newFullPath = Path.Combine(_uploadPath, newRelativePath);

                // Move file
                File.Move(oldPath, newFullPath, overwrite: true);

                // Update database with new path
                file.DosyaYolu = newRelativePath;
                file.DuyuruId = announcementId;

                _logger.LogInformation("File moved from {OldPath} to {NewPath}", oldPath, newFullPath);

                // Clean up empty directories
                CleanupEmptyDirectories(Path.GetDirectoryName(oldPath)!);
            }
            else
            {
                // File doesn't exist in temp, just update DuyuruId
                file.DuyuruId = announcementId;
                _logger.LogWarning("File {FileId} not found at {Path}, only updating DuyuruId", fileId, oldPath);
            }

            announcement.EkDosyalar.Add(file);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileId} attached to announcement {AnnouncementId} by user {UserId}",
                fileId, announcementId, kullaniciId);

            return ResponseModel.SuccessResult("Dosya duyuruya başarıyla eklendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching file {FileId} to announcement {AnnouncementId}", fileId, announcementId);
            return ResponseModel.ErrorResult("Dosya duyuruya eklenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DetachFileFromAnnouncementAsync(int fileId, int announcementId, int kullaniciId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.EkDosyalar)
                .FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            var file = announcement.EkDosyalar.FirstOrDefault(f => f.Id == fileId);
            if (file == null)
                return ResponseModel.ErrorResult("Dosya bu duyuruda bulunamadı", 404);

            // Move file back to temp folder
            var oldPath = Path.Combine(_uploadPath, file.DosyaYolu);

            if (File.Exists(oldPath))
            {
                // Create temp folder for user
                var tempFolder = Path.Combine(_uploadPath, "temp", $"user-{file.YukleyenKullaniciId}");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                var fileName = Path.GetFileName(file.DosyaYolu);
                var newRelativePath = Path.Combine("temp", $"user-{file.YukleyenKullaniciId}", fileName);
                var newFullPath = Path.Combine(_uploadPath, newRelativePath);

                // Move file back to temp
                File.Move(oldPath, newFullPath, overwrite: true);

                // Update database
                file.DosyaYolu = newRelativePath;
                file.DuyuruId = null;

                _logger.LogInformation("File moved back from {OldPath} to {NewPath}", oldPath, newFullPath);
            }
            else
            {
                // File doesn't exist, just update DuyuruId
                file.DuyuruId = null;
                _logger.LogWarning("File {FileId} not found at {Path}, only updating DuyuruId", fileId, oldPath);
            }

            announcement.EkDosyalar.Remove(file);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileId} detached from announcement {AnnouncementId} by user {UserId}",
                fileId, announcementId, kullaniciId);

            return ResponseModel.SuccessResult("Dosya duyurudan başarıyla çıkarıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching file {FileId} from announcement {AnnouncementId}", fileId, announcementId);
            return ResponseModel.ErrorResult("Dosya duyurudan çıkarılırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return ResponseModel.ErrorResult("Dosya seçilmedi", 400);

        if (file.Length > _maxFileSize)
            return ResponseModel.ErrorResult($"Dosya boyutu çok büyük. Maksimum {_maxFileSize / (1024 * 1024)} MB olabilir", 400);

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(fileExtension))
            return ResponseModel.ErrorResult($"Desteklenmeyen dosya türü: {fileExtension}", 400);

        // Check for potentially dangerous content (config'den oku)
        var dangerousExtensions = _fileSettings.GetDangerousExtensionsArray();
        if (dangerousExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("SECURITY: Dangerous file extension blocked: {Extension}", fileExtension);
            return ResponseModel.ErrorResult("Güvenlik nedeniyle bu dosya türü yüklenemez", 400);
        }

        // Content-based validation for common file types
        var contentValidation = await ValidateFileContentAsync(file);
        if (!contentValidation.Success)
            return contentValidation;

        return await Task.FromResult(ResponseModel.SuccessResult("Dosya geçerli"));
    }

    private async Task<ResponseModel> ValidateFileContentAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[512]; // Read first 512 bytes for magic number check
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

            if (bytesRead == 0)
                return ResponseModel.ErrorResult("Dosya boş veya okunamıyor", 400);

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            // Validate file signatures (magic numbers) - MIME type check
            switch (fileExtension)
            {
                case ".pdf":
                    if (bytesRead >= 4 &&
                        !(buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)) // %PDF
                        return ResponseModel.ErrorResult("PDF dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".jpg":
                case ".jpeg":
                    if (bytesRead >= 3 &&
                        !(buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)) // JPEG
                        return ResponseModel.ErrorResult("JPEG dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".png":
                    if (bytesRead >= 8 &&
                        !(buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                          buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)) // PNG
                        return ResponseModel.ErrorResult("PNG dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".gif":
                    if (bytesRead >= 6 &&
                        !((buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38 &&
                          (buffer[4] == 0x37 || buffer[4] == 0x39) && buffer[5] == 0x61))) // GIF87a or GIF89a
                        return ResponseModel.ErrorResult("GIF dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".doc":
                    if (bytesRead >= 8 &&
                        !(buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
                          buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)) // MS Office DOC
                        return ResponseModel.ErrorResult("DOC dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".docx":
                case ".xlsx":
                case ".pptx":
                    // Office Open XML formats (ZIP-based)
                    if (bytesRead >= 4 &&
                        !(buffer[0] == 0x50 && buffer[1] == 0x4B && (buffer[2] == 0x03 || buffer[2] == 0x05 || buffer[2] == 0x07) && (buffer[3] == 0x04 || buffer[3] == 0x06 || buffer[3] == 0x08))) // PK (ZIP)
                        return ResponseModel.ErrorResult($"{fileExtension.ToUpper()} dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".xls":
                    if (bytesRead >= 8 &&
                        !(buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
                          buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)) // MS Office XLS
                        return ResponseModel.ErrorResult("XLS dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".ppt":
                    if (bytesRead >= 8 &&
                        !(buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
                          buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)) // MS Office PPT
                        return ResponseModel.ErrorResult("PPT dosya içeriği geçersiz (MIME type uyuşmuyor)", 400);
                    break;

                case ".txt":
                    // Text files - no specific magic number, just verify it's readable text
                    var isValidText = buffer.Take(Math.Min(bytesRead, 100))
                        .All(b => b == 0x09 || b == 0x0A || b == 0x0D || (b >= 0x20 && b <= 0x7E) || b >= 0x80);
                    if (!isValidText)
                        return ResponseModel.ErrorResult("TXT dosya içeriği geçersiz (okunamayan karakterler)", 400);
                    break;
            }

            // Check for suspicious content patterns (basic detection)
            var content = System.Text.Encoding.UTF8.GetString(buffer).ToLower();
            var suspiciousPatterns = new[] { "<script", "javascript:", "vbscript:", "onload=", "eval(", "cmd.exe" };

            if (suspiciousPatterns.Any(pattern => content.Contains(pattern)))
                return ResponseModel.ErrorResult("Dosya içeriği güvenlik taramasından geçemedi", 400);

            return ResponseModel.SuccessResult("İçerik geçerli");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "File content validation failed for {FileName}", file.FileName);
            return ResponseModel.ErrorResult("Dosya içeriği doğrulanamadı", 400);
        }
    }

    public async Task<ResponseDataModel<FileInfo>> GetFileInfoAsync(int id)
    {
        try
        {
            var file = await _context.Dosyalar
                .Include(f => f.YukleyenKullanici)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return ResponseDataModel<FileInfo>.ErrorResult("Dosya bulunamadı", 404);

            var fileInfo = new FileInfo
            {
                Id = file.Id,
                FileName = file.DosyaAdi,
                FileSize = file.DosyaBoyutu,
                FileType = file.DosyaTipi,
                MimeType = file.DosyaTipi,
                Description = file.Aciklama,
                UploadDate = file.YuklemeTarihi,
                UploaderName = file.YukleyenKullanici?.AdSoyad,
                DownloadCount = file.IndirmeSayisi ?? 0,
                LastDownloadDate = file.SonIndirmeTarihi
            };

            return ResponseDataModel<FileInfo>.SuccessResult(fileInfo, "Dosya bilgileri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info {Id}", id);
            return ResponseDataModel<FileInfo>.ErrorResult("Dosya bilgileri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<Dosya>>> GetSessionFilesAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return ResponseDataModel<List<Dosya>>.ErrorResult("Session ID gerekli", 400);

            var files = await _context.Dosyalar
                .Where(f => f.SessionId == sessionId && f.Aktif == "Y")
                .Include(f => f.YukleyenKullanici)
                .OrderBy(f => f.YuklemeTarihi)
                .ToListAsync();

            return ResponseDataModel<List<Dosya>>.SuccessResult(files, $"{files.Count} session dosyası bulundu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session files for session {SessionId}", sessionId);
            return ResponseDataModel<List<Dosya>>.ErrorResult("Session dosyaları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> LinkSessionFilesToAnnouncementAsync(string sessionId, int announcementId, int kullaniciId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return ResponseModel.ErrorResult("Session ID gerekli", 400);

            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);
            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // Get session files
            var sessionFiles = await _context.Dosyalar
                .Where(f => f.SessionId == sessionId && f.DuyuruId == null && f.Aktif == "Y")
                .ToListAsync();

            if (sessionFiles.Count == 0)
            {
                _logger.LogInformation("No session files found for session {SessionId}", sessionId);
                return ResponseModel.SuccessResult("Session dosyası bulunamadı");
            }

            // Paralel dosya bağlama (max 4 eşzamanlı)
            var semaphore = new SemaphoreSlim(4, 4);
            var attachTasks = sessionFiles.Select(async file =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var attachResult = await AttachFileToAnnouncementAsync(file.Id, announcementId, kullaniciId);
                    return (file, attachResult);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var attachResults = await Task.WhenAll(attachTasks);

            var linkedCount = 0;
            foreach (var (file, attachResult) in attachResults)
            {
                if (attachResult.Success)
                {
                    // Clear session ID after successful attachment
                    file.SessionId = null;
                    linkedCount++;
                }
                else
                {
                    _logger.LogWarning("Failed to attach file {FileId} from session {SessionId}: {Message}",
                        file.Id, sessionId, attachResult.Message);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Count} session files linked to announcement {AnnouncementId} from session {SessionId}",
                linkedCount, announcementId, sessionId);

            return ResponseModel.SuccessResult($"{linkedCount} dosya duyuruya başarıyla bağlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking session files from {SessionId} to announcement {AnnouncementId}",
                sessionId, announcementId);
            return ResponseModel.ErrorResult("Session dosyaları bağlanırken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Recursively clean up empty directories from the given path up to the upload root
    /// </summary>
    private void CleanupEmptyDirectories(string directoryPath)
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return;

            // Don't delete the upload root or its immediate subdirectories (temp, announcements, etc.)
            if (directoryPath == _uploadPath ||
                Path.GetDirectoryName(directoryPath) == _uploadPath)
                return;

            // If directory is empty, delete it and check parent
            if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
                _logger.LogInformation("Deleted empty directory: {DirectoryPath}", directoryPath);

                // Recursively check parent directory
                var parentDir = Path.GetDirectoryName(directoryPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    CleanupEmptyDirectories(parentDir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup directory {DirectoryPath}", directoryPath);
        }
    }
}

// DTOs
public class FileUploadResult
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}

public class FileDownloadResult : IDisposable
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[]? FileContent { get; set; } // For small files
    public Stream? FileStream { get; set; } // For large files
    public long FileSize { get; set; }

    public void Dispose()
    {
        FileStream?.Dispose();
    }
}

public class FileInfo
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string? Description { get; set; }
    public DateTime UploadDate { get; set; }
    public string? UploaderName { get; set; }
    public int DownloadCount { get; set; }
    public DateTime? LastDownloadDate { get; set; }
}