using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using DeuEposta.Utils;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface IAnnouncementApprovalService
{
    Task<ResponseDataModel<List<PendingApprovalView>>> GetPendingApprovalsAsync(int page = 1, int pageSize = 20, int currentUserId = 0, bool isAdmin = false, bool isCoordinator = false, bool isManager = false);

    Task<ResponseDataModel<List<ApprovedAnnouncementView>>> GetApprovedAnnouncementsAsync(int page = 1, int pageSize = 20, int currentUserId = 0, bool isCoordinator = false, bool isManager = false);

    Task<ResponseDataModel<List<ApprovedAnnouncementView>>> GetRejectedAnnouncementsAsync(int page = 1, int pageSize = 20, int currentUserId = 0, bool isCoordinator = false, bool isManager = false);

    Task<ResponseDataModel<bool>> CanApproveAsync(int id, bool isAdmin);

    Task<ResponseDataModel<bool>> CanSendAsync(int id, bool canSend);

    Task<ResponseModel> SendAnnouncementAsync(int id, int kullaniciId);

    Task<ResponseModel> ApproveAndSendAnnouncementAsync(int id, int kullaniciId);

    Task ProcessSendAnnouncementJob(int id, int kullaniciId);
}

public class AnnouncementApprovalService : IAnnouncementApprovalService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AnnouncementApprovalService> _logger;
    private readonly IEmailService _emailService;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IAuditLogService _auditLog;
    private readonly string _uploadPath;

    public AnnouncementApprovalService(
        DeuEpostaContext context,
        ILogger<AnnouncementApprovalService> logger,
        IEmailService emailService,
        ISystemSettingsService systemSettingsService,
        IAuditLogService auditLog,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _systemSettingsService = systemSettingsService;
        _auditLog = auditLog;

        var uploadPath = configuration["FileSettings:UploadPath"] ?? "uploads";
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);
    }

    public async Task<ResponseDataModel<List<PendingApprovalView>>> GetPendingApprovalsAsync(int page = 1, int pageSize = 20, int currentUserId = 0, bool isAdmin = false, bool isCoordinator = false, bool isManager = false)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            // PERFORMANS: Include(Hareketler) + ThenInclude(SecilenOnaylayici) ile N+1 query önlenir
            var query = _context.EpostaDuyurulari
                .AsNoTracking() // Read-only query
                .Include(d => d.OlusturanKullanici)
                .Include(d => d.IlkOnaylayanKullanici)  // Koordinatör bilgisi
                .Include(d => d.SonOnaylayanKullanici)
                .Include(d => d.Alicilar)
                .Include(d => d.Hareketler.Where(h => h.IslemTipi == "ONAYLAMA" && h.YeniDurum == DuyuruDurum.SON_ONAY_BEKLIYOR))
                    .ThenInclude(h => h.SecilenOnaylayici) // FIX: N+1 query önlenir
                .Where(d => d.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR || d.Durum == DuyuruDurum.SON_ONAY_BEKLIYOR);

            // Rol bazlı filtreleme
            if (isAdmin)
            {
                // ADMIN: Tüm onay bekleyen duyuruları göster (filtreleme yok)
            }
            else if (isCoordinator)
            {
                // COORDINATOR: Sadece ILK_ONAY_BEKLIYOR durumundaki duyuruları göster
                query = query.Where(d => d.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR);
            }
            else if (isManager)
            {
                // MANAGER: Sadece kendisine atanan SON_ONAY_BEKLIYOR duyuruları göster
                query = query.Where(d => d.SonOnaylayanKullaniciId == currentUserId && d.Durum == DuyuruDurum.SON_ONAY_BEKLIYOR);
            }

            var totalCount = await query.CountAsync();

            var pendingAnnouncements = await query
                .OrderBy(d => d.OlusturmaTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new PendingApprovalView
                {
                    Id = d.Id,
                    Konu = d.Konu,
                    IcerikTipi = d.IcerikTipi,
                    Durum = d.Durum,
                    OlusturanKullaniciId = d.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = d.OlusturanKullanici != null ? d.OlusturanKullanici.AdSoyad : null,
                    // İki aşamalı onay bilgileri
                    IlkOnaylayanKullaniciId = d.IlkOnaylayanKullaniciId,  // Koordinatör
                    IlkOnaylayanKullaniciAdi = d.IlkOnaylayanKullanici != null ? d.IlkOnaylayanKullanici.AdSoyad : null,
                    SonOnaylayanKullaniciId = d.SonOnaylayanKullaniciId,  // Manager (atanan)
                    SonOnaylayanKullaniciAdi = d.SonOnaylayanKullanici != null ? d.SonOnaylayanKullanici.AdSoyad : null,
                    // Backward compatibility
                    OnaylayanKullaniciId = d.SonOnaylayanKullaniciId, // Manager bilgisi
                    OnaylayanKullaniciAdi = d.SonOnaylayanKullanici != null ? d.SonOnaylayanKullanici.AdSoyad : null,
                    // PERFORMANS: Hareketler Include edildiği için memory'de filtrele (N+1 query önlendi)
                    OnayNotu = d.Hareketler
                        .Where(h => h.IslemTipi == "ONAYLAMA" && h.YeniDurum == DuyuruDurum.SON_ONAY_BEKLIYOR)
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.Aciklama)
                        .FirstOrDefault(),
                    ToplamAliciSayisi = d.Alicilar.Count,
                    OlusturmaTarihi = d.OlusturmaTarihi,
                    GuncellemeTarihi = d.GuncellemeTarihi
                })
                .ToListAsync();

            return ResponseDataModel<List<PendingApprovalView>>.SuccessResultWithPagination(
                pendingAnnouncements, totalCount, page, pageSize, "Onay bekleyen duyurular alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return ResponseDataModel<List<PendingApprovalView>>.ErrorResult("Onay bekleyen duyurular alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ApprovedAnnouncementView>>> GetApprovedAnnouncementsAsync(int page = 1, int pageSize = 20, int currentUserId = 0, bool isCoordinator = false, bool isManager = false)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            var query = _context.EpostaDuyurulari
                .Include(d => d.OlusturanKullanici)
                .Include(d => d.SonOnaylayanKullanici)
                .Include(d => d.IlkOnaylayanKullanici)
                .Include(d => d.Hareketler)
                    .ThenInclude(h => h.Kullanici)
                .AsQueryable();

            // Rol bazlı filtreleme
            if (isCoordinator && currentUserId > 0)
            {
                // Koordinatör: İlk onayladığı duyurular (SON_ONAY_BEKLIYOR, ONAYLANDI, GONDERILDI)
                query = query.Where(d => d.IlkOnaylayanKullaniciId == currentUserId &&
                                        (d.Durum == DuyuruDurum.SON_ONAY_BEKLIYOR ||
                                         d.Durum == DuyuruDurum.ONAYLANDI ||
                                         d.Durum == DuyuruDurum.GONDERILDI));
            }
            else if (isManager && currentUserId > 0)
            {
                // Manager: Son onayladığı duyurular (ONAYLANDI, GONDERILDI)
                query = query.Where(d => d.SonOnaylayanKullaniciId == currentUserId &&
                                        (d.Durum == DuyuruDurum.ONAYLANDI ||
                                         d.Durum == DuyuruDurum.GONDERILDI));
            }
            else
            {
                // Admin veya filtresiz: Sadece ONAYLANDI durumu
                query = query.Where(d => d.Durum == DuyuruDurum.ONAYLANDI);
            }

            var totalCount = await query.CountAsync();

            // Hareket tablosu ile join yaparak gerçek işlem bilgilerini çek
            var approvedAnnouncementsWithMovements = await query
                .OrderByDescending(d => d.GuncellemeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    Announcement = d,
                    // Koordinatör onay hareketini bul (ILK_ONAY_BEKLIYOR → SON_ONAY_BEKLIYOR)
                    CoordinatorApproval = d.Hareketler
                        .Where(h => h.IslemTipi == "ONAYLAMA" && h.YeniDurum == DuyuruDurum.SON_ONAY_BEKLIYOR)
                        .OrderByDescending(h => h.IslemTarihi)
                        .FirstOrDefault(),
                    // Manager onay hareketini bul (SON_ONAY_BEKLIYOR → ONAYLANDI)
                    ManagerApproval = d.Hareketler
                        .Where(h => h.IslemTipi == "ONAYLAMA" && h.YeniDurum == DuyuruDurum.ONAYLANDI)
                        .OrderByDescending(h => h.IslemTarihi)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // DTO'ya map et - rol bazlı işlem bilgilerini göster
            var result = approvedAnnouncementsWithMovements.Select(x =>
            {
                var announcement = x.Announcement;
                DateTime? islemTarihi = null;
                string? islemNotu = null;
                string? islemYapan = null;

                // Koordinatör için: İlk onay bilgilerini göster
                if (isCoordinator && x.CoordinatorApproval != null)
                {
                    islemTarihi = x.CoordinatorApproval.IslemTarihi;
                    islemNotu = x.CoordinatorApproval.Aciklama;
                    islemYapan = announcement.IlkOnaylayanKullanici?.AdSoyad;
                }
                // Manager için: Son onay bilgilerini göster
                else if (isManager && x.ManagerApproval != null)
                {
                    islemTarihi = x.ManagerApproval.IslemTarihi;
                    islemNotu = x.ManagerApproval.Aciklama;
                    islemYapan = announcement.SonOnaylayanKullanici?.AdSoyad;
                }

                return new ApprovedAnnouncementView
                {
                    Id = announcement.Id,
                    Konu = announcement.Konu,
                    Durum = announcement.Durum,
                    OlusturanKullaniciId = announcement.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = announcement.OlusturanKullanici?.AdSoyad,
                    // İki aşamalı onay bilgileri
                    IlkOnaylayanKullaniciId = announcement.IlkOnaylayanKullaniciId,
                    IlkOnaylayanKullaniciAdi = announcement.IlkOnaylayanKullanici?.AdSoyad,
                    SonOnaylayanKullaniciId = announcement.SonOnaylayanKullaniciId,
                    SonOnaylayanKullaniciAdi = announcement.SonOnaylayanKullanici?.AdSoyad,
                    // Backward compatibility
                    OnaylayanKullaniciId = announcement.SonOnaylayanKullaniciId,
                    OnaylayanKullaniciAdi = announcement.SonOnaylayanKullanici?.AdSoyad,
                    // İşlem bilgileri (Hareket tablosundan)
                    IslemTarihi = islemTarihi,
                    IslemNotu = islemNotu,
                    IslemYapan = islemYapan,
                    // Tarih bilgileri
                    OlusturmaTarihi = announcement.OlusturmaTarihi,
                    OnayTarihi = islemTarihi ?? announcement.GuncellemeTarihi,
                    GuncellemeTarihi = announcement.GuncellemeTarihi
                };
            }).ToList();

            return ResponseDataModel<List<ApprovedAnnouncementView>>.SuccessResultWithPagination(
                result, totalCount, page, pageSize, "Onaylanmış duyurular alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approved announcements");
            return ResponseDataModel<List<ApprovedAnnouncementView>>.ErrorResult("Onaylanmış duyurular alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ApprovedAnnouncementView>>> GetRejectedAnnouncementsAsync(int page = 1, int pageSize = 20, int currentUserId = 0, bool isCoordinator = false, bool isManager = false)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            // Hareket tablosundan REDDETME işlemlerini çek
            var rejectedMovements = await _context.EpostaDuyuruHareketleri
                .Where(h => h.KullaniciId == currentUserId && h.IslemTipi == "REDDETME")
                .Select(h => h.DuyuruId)
                .Distinct()
                .ToListAsync();

            if (!rejectedMovements.Any())
            {
                return ResponseDataModel<List<ApprovedAnnouncementView>>.SuccessResultWithPagination(
                    new List<ApprovedAnnouncementView>(), 0, page, pageSize, "Reddedilen duyuru bulunamadı");
            }

            var query = _context.EpostaDuyurulari
                .Include(d => d.OlusturanKullanici)
                .Include(d => d.SonOnaylayanKullanici)
                .Include(d => d.IlkOnaylayanKullanici)
                .Include(d => d.Hareketler)
                    .ThenInclude(h => h.Kullanici)
                .Where(d => rejectedMovements.Contains(d.Id));

            var totalCount = await query.CountAsync();

            // Hareket tablosu ile join yaparak gerçek işlem bilgilerini çek
            var rejectedAnnouncementsWithMovements = await query
                .OrderByDescending(d => d.GuncellemeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    Announcement = d,
                    // Red hareketini bul (current user tarafından yapılan)
                    RejectionMovement = d.Hareketler
                        .Where(h => h.IslemTipi == "REDDETME" && h.KullaniciId == currentUserId)
                        .OrderByDescending(h => h.IslemTarihi)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // DTO'ya map et
            var result = rejectedAnnouncementsWithMovements.Select(x =>
            {
                var announcement = x.Announcement;
                var rejection = x.RejectionMovement;

                return new ApprovedAnnouncementView
                {
                    Id = announcement.Id,
                    Konu = announcement.Konu,
                    Durum = announcement.Durum,
                    OlusturanKullaniciId = announcement.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = announcement.OlusturanKullanici?.AdSoyad,
                    // İki aşamalı onay bilgileri
                    IlkOnaylayanKullaniciId = announcement.IlkOnaylayanKullaniciId,
                    IlkOnaylayanKullaniciAdi = announcement.IlkOnaylayanKullanici?.AdSoyad,
                    SonOnaylayanKullaniciId = announcement.SonOnaylayanKullaniciId,
                    SonOnaylayanKullaniciAdi = announcement.SonOnaylayanKullanici?.AdSoyad,
                    // Backward compatibility
                    OnaylayanKullaniciId = announcement.SonOnaylayanKullaniciId,
                    OnaylayanKullaniciAdi = announcement.SonOnaylayanKullanici?.AdSoyad,
                    // İşlem bilgileri (Red hareketinden)
                    IslemTarihi = rejection?.IslemTarihi,
                    IslemNotu = rejection?.Aciklama,  // Red notu
                    IslemYapan = rejection?.Kullanici?.AdSoyad,  // Red eden kişi
                    // Tarih bilgileri
                    OlusturmaTarihi = announcement.OlusturmaTarihi,
                    OnayTarihi = rejection?.IslemTarihi ?? announcement.GuncellemeTarihi,
                    GuncellemeTarihi = announcement.GuncellemeTarihi
                };
            }).ToList();

            return ResponseDataModel<List<ApprovedAnnouncementView>>.SuccessResultWithPagination(
                result, totalCount, page, pageSize, "Reddedilen duyurular alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rejected announcements");
            return ResponseDataModel<List<ApprovedAnnouncementView>>.ErrorResult("Reddedilen duyurular alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<bool>> CanApproveAsync(int id, bool isAdmin)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseDataModel<bool>.ErrorResult("Duyuru bulunamadı", 404);

            bool canApprove = isAdmin && (announcement.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR || announcement.Durum == DuyuruDurum.SON_ONAY_BEKLIYOR);

            return ResponseDataModel<bool>.SuccessResult(canApprove, "Onaylama yetkisi kontrol edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking approval permission for announcement {Id}", id);
            return ResponseDataModel<bool>.ErrorResult("Onaylama yetkisi kontrol edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<bool>> CanSendAsync(int id, bool canSend)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseDataModel<bool>.ErrorResult("Duyuru bulunamadı", 404);

            // Check: user has send permission AND announcement is ONAYLANDI
            bool isApproved = announcement.Durum == DuyuruDurum.ONAYLANDI;
            bool canSendAnnouncement = canSend && isApproved;

            return ResponseDataModel<bool>.SuccessResult(canSendAnnouncement, "Gönderme yetkisi kontrol edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking send permission for announcement {Id}", id);
            return ResponseDataModel<bool>.ErrorResult("Gönderme yetkisi kontrol edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> SendAnnouncementAsync(int id, int kullaniciId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.ONAYLANDI)
                return ResponseModel.ErrorResult("Sadece onaylanmış duyurular gönderilebilir", 400);

            if (!announcement.Alicilar.Any())
                return ResponseModel.ErrorResult("Alıcı listesi boş, duyuru gönderilemez", 400);

            // KRİTİK: EDITOR sadece kendi duyurusunu gönderebilir (ADMIN/MANAGER hariç)
            var sender = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == kullaniciId);

            var isAdmin = sender?.Rol?.RolKodu == "ADMIN";
            var isManager = sender?.Rol?.RolKodu == "MANAGER";
            var isEditor = sender?.Rol?.RolKodu == "EDITOR";

            if (isEditor && announcement.OlusturanKullaniciId != kullaniciId)
            {
                return ResponseModel.ErrorResult("Sadece kendi oluşturduğunuz duyuruları gönderebilirsiniz", 403);
            }

            // Enqueue for immediate execution (scheduling is handled by ScheduleService)
            BackgroundJob.Enqueue<IAnnouncementApprovalService>(s => s.ProcessSendAnnouncementJob(id, kullaniciId));

            _logger.LogInformation("Announcement enqueued for immediate sending: {Id} by user {UserId} from ",
                id, kullaniciId);

            // Audit log: Anında gönderim
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_GONDERIME_ALMA",
                detay: $"Duyuru anında gönderim kuyruğuna alındı. ID: {id}, Konu: {announcement.Konu}, Alıcı Sayısı: {announcement.Alicilar.Count}"
            );

            return ResponseModel.SuccessResult("Duyuru hemen gönderilmek üzere kuyruğa alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru gönderilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ApproveAndSendAnnouncementAsync(int id, int kullaniciId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // TASLAK veya ONAY_BEKLIYOR durumunda olmalı
            if (announcement.Durum != DuyuruDurum.TASLAK && announcement.Durum != DuyuruDurum.ILK_ONAY_BEKLIYOR && announcement.Durum != DuyuruDurum.SON_ONAY_BEKLIYOR)
                return ResponseModel.ErrorResult("Bu duyuru onaylanıp gönderilemez", 400);

            // Alıcı kontrolü
            if (!announcement.Alicilar.Any())
                return ResponseModel.ErrorResult("Alıcı listesi boş, duyuru gönderilemez", 400);

            // Durumu ONAYLANDI yap
            var oncekiDurum = announcement.Durum;
            announcement.Durum = DuyuruDurum.ONAYLANDI;
            // Legacy: Eğer ILK_ONAY_BEKLIYOR ise IlkOnaylayan, eğer SON_ONAY_BEKLIYOR ise SonOnaylayan set et
            if (oncekiDurum == DuyuruDurum.ILK_ONAY_BEKLIYOR)
                announcement.IlkOnaylayanKullaniciId = kullaniciId;
            else if (oncekiDurum == DuyuruDurum.SON_ONAY_BEKLIYOR)
                announcement.SonOnaylayanKullaniciId = kullaniciId;
            else if (oncekiDurum == DuyuruDurum.TASLAK)
            {
                // TASLAK'tan direkt gönderim - her ikisine de set et
                announcement.IlkOnaylayanKullaniciId = kullaniciId;
                announcement.SonOnaylayanKullaniciId = kullaniciId;
            }
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı ekle
            _context.EpostaDuyuruHareketleri.Add(new EpostaDuyuruHareket
            {
                DuyuruId = id,
                OncekiDurum = oncekiDurum,
                YeniDurum = DuyuruDurum.ONAYLANDI,
                IslemTipi = "ONAYLAMA",
                KullaniciId = kullaniciId,
                Aciklama = "ADMIN/MANAGER tarafından direkt onaylandı ve gönderildi",
                IslemTarihi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement approved and queued for sending: {Id} by user {UserId} from", id, kullaniciId);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_ONAYLA_GONDER",
                detay: $"Duyuru ADMIN/MANAGER tarafından direkt onaylanıp gönderildi. ID: {id}, Konu: {announcement.Konu}, Alıcı Sayısı: {announcement.Alicilar.Count}"
            );

            // Gönderim kuyruğuna al (bildirim emaili GÖNDERİLMEZ)
            BackgroundJob.Enqueue<IAnnouncementApprovalService>(s => s.ProcessSendAnnouncementJob(id, kullaniciId));

            return ResponseModel.SuccessResult("Duyuru onaylandı ve gönderilmek üzere kuyruğa alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving and sending announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru onaylanıp gönderilirken hata oluştu", 500);
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)] // Prevent duplicate execution for 1 hour
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })] // Retry 3 times: 1min, 5min, 15min
    public async Task ProcessSendAnnouncementJob(int id, int kullaniciId)
    {
        _logger.LogInformation("🕐 Scheduled job started for announcement {AnnouncementId} by user {UserId}", id, kullaniciId);

        try
        {
            // 1) ATOMIK UPDATE: Race condition ve iptal kontrolü
            // İki Hangfire job aynı anda çalışırsa sadece biri GONDERILIYOR durumuna geçebilir
            // İptal edilmiş duyurular gönderilmez (IPTAL state check)
            using var transaction = await _context.Database.BeginTransactionAsync();

            var rowsUpdated = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE EPOSTA_DUYURULARI SET DURUM = {0} WHERE ID = {1} AND DURUM = {2}",
                DuyuruDurum.GONDERILIYOR, id, DuyuruDurum.ONAYLANDI);

            if (rowsUpdated == 0)
            {
                // KRİTİK: Duyuru ONAYLANDI durumunda değil (IPTAL olabilir!)
                var currentAnnouncement = await _context.EpostaDuyurulari
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (currentAnnouncement?.Durum == DuyuruDurum.IPTAL)
                {
                    _logger.LogInformation("Send job CANCELLED: Announcement {Id} is in IPTAL state, aborting send", id);
                }
                else
                {
                    _logger.LogWarning("Send job: Announcement {Id} already being sent or not in ONAYLANDI state. Current: {Status}",
                        id, currentAnnouncement?.Durum);
                }

                await transaction.RollbackAsync();
                return;
            }

            // Hareket kaydı: Gönderim başlatıldı (ONAYLANDI → GONDERILIYOR) - transaction içinde
            AddHareket(id, DuyuruDurum.ONAYLANDI, DuyuruDurum.GONDERILIYOR,
                "GONDERIM", kullaniciId, "Email gönderimi başlatıldı", null);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // 2) Re-fetch with includes
            var announcement = await _context.EpostaDuyurulari
                .AsSplitQuery() // Performance: Her collection için ayrı query (warning'i kaldırır)
                .Include(d => d.Alicilar)
                .Include(d => d.EkDosyalar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
            {
                _logger.LogError("Send job: Announcement not found after atomic update {Id}", id);
                return;
            }

            // 3) Build recipient lists (deduped)
            var toSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ccSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bccSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // PERFORMANS: Grup email'lerini önce topla (N+1 query'yi önle)
            var uniqueGroupIds = announcement.Alicilar
                .Where(a => a.GrupId.HasValue)
                .Select(a => new { GroupId = a.GrupId!.Value, Category = a.AliciKategorisi })
                .Distinct()
                .ToList();

            // Paralel grup email çözümlemesi (performans optimizasyonu)
            var emailTasks = uniqueGroupIds.Select(async groupInfo =>
            {
                var emails = await _emailService.GetSmartRecipientsAsync(groupInfo.GroupId, groupInfo.Category);
                return (groupInfo, emails);
            });
            var emailResults = await Task.WhenAll(emailTasks);

            var groupEmailsCache = emailResults.ToDictionary(
                r => (r.groupInfo.GroupId, r.groupInfo.Category),
                r => r.emails
            );

            _logger.LogInformation("Loaded {Count} unique group-category combinations for announcement {Id}",
                groupEmailsCache.Count, id);

            // Şimdi cache'den oku
            foreach (var recipient in announcement.Alicilar)
            {
                if (recipient.GrupId.HasValue)
                {
                    var key = (recipient.GrupId.Value, recipient.AliciKategorisi);
                    if (groupEmailsCache.TryGetValue(key, out var emails))
                    {
                        foreach (var e in emails)
                        {
                            AddByCategory(recipient.AliciKategorisi, e, toSet, ccSet, bccSet);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Group {GroupId} with category {Category} not found in cache",
                            recipient.GrupId.Value, recipient.AliciKategorisi);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(recipient.Email))
                {
                    AddByCategory(recipient.AliciKategorisi, recipient.Email, toSet, ccSet, bccSet);
                }
            }

            // Banner ve imza ekle
            var emailBody = announcement.Icerik;

            // Banner varsa HTML'in başına ekle (Base64 encoded)
            if (announcement.BannerDosyaId.HasValue)
            {
                var bannerFile = await _context.Dosyalar.FirstOrDefaultAsync(f => f.Id == announcement.BannerDosyaId.Value);
                if (bannerFile != null)
                {
                    try
                    {
                        // Dosyayı oku ve base64'e çevir
                        var filePath = Path.Combine(_uploadPath, bannerFile.DosyaYolu);
                        if (File.Exists(filePath))
                        {
                            var fileBytes = await File.ReadAllBytesAsync(filePath);
                            var base64Image = Convert.ToBase64String(fileBytes);
                            var mimeType = bannerFile.DosyaTipi ?? "image/png";
                            var dataUrl = $"data:{mimeType};base64,{base64Image}";

                            emailBody = $"<div style='text-align:center;margin-bottom:20px;'><img src='{dataUrl}' alt='Banner' style='max-width:100%;height:auto;'/></div>{emailBody}";
                            _logger.LogInformation("Banner {BannerId} embedded as base64 in email for announcement {AnnouncementId}", bannerFile.Id, announcement.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Banner file not found at path: {Path}", filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to embed banner {BannerId} in email", bannerFile.Id);
                    }
                }
            }

            // İmza ekle (kategori bazlı) - YENİ YAPI: EMAIL_KATEGORI içinde
            var signature = await _systemSettingsService.GetEmailSignatureAsync(announcement.DuyuruKategorisi);
            if (!string.IsNullOrEmpty(signature))
            {
                emailBody += signature;
                _logger.LogInformation("Signature for category {Category} added to email for announcement {AnnouncementId}",
                    announcement.DuyuruKategorisi, announcement.Id);
            }

            // Ek dosyalar varsa attach et
            var attachments = new List<EmailAttachment>();
            if (announcement.EkDosyalar?.Any() == true)
            {
                foreach (var dosya in announcement.EkDosyalar)
                {
                    var filePath = Path.Combine(_uploadPath, dosya.DosyaYolu);
                    if (File.Exists(filePath))
                    {
                        attachments.Add(new EmailAttachment
                        {
                            FileName = dosya.DosyaAdi,
                            FilePath = filePath,
                            ContentType = dosya.DosyaTipi ?? "application/octet-stream"
                        });
                        _logger.LogInformation("Attachment {FileName} ({FileId}) added to announcement {AnnouncementId}",
                            dosya.DosyaAdi, dosya.Id, announcement.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Attachment file not found: {FilePath} for file {FileId}",
                            filePath, dosya.Id);
                    }
                }
            }

            var request = new SendEmailRequest
            {
                Subject = announcement.Konu,
                Body = emailBody,
                IsHtml = true,
                ToRecipients = toSet.ToList(),
                CcRecipients = ccSet.ToList(),
                BccRecipients = bccSet.ToList(),
                Category = announcement.GondericiKategori, // SMTP gönderici kategorisi (EMAIL_PERSONEL, EMAIL_REKTORLUK, vb.)
                Attachments = attachments
            };

            // 4) Send outside DB transaction
            var sent = await _emailService.SendEmailAsync(request);

            // 5) Log each recipient and update DB with final status (in transaction)
            try
            {
                await using var loggingTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var finalAnnouncement = await _context.EpostaDuyurulari
                        .Include(d => d.Alicilar)
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (finalAnnouncement != null)
                    {
                        var gonderimTarihi = DateTime.Now;
                        var gonderimDurumu = sent ? "GONDERILDI" : "BASARISIZ";
                        var hataMesaji = sent ? null : "Email servisi gönderim hatası";

                        // Log each recipient
                        var sendLogs = new List<EpostaDuyuruGonderimLog>();

                        // TO Recipients
                        foreach (var email in request.ToRecipients)
                        {
                            sendLogs.Add(new EpostaDuyuruGonderimLog
                            {
                                DuyuruId = id,
                                AliciEmail = email,
                                AliciKategorisi = "TO",
                                GonderimDurumu = sent ? "BASARILI" : "BASARISIZ",
                                HataMesaji = hataMesaji,
                                GonderimTarihi = gonderimTarihi
                            });
                        }

                        // CC Recipients
                        foreach (var email in request.CcRecipients)
                        {
                            sendLogs.Add(new EpostaDuyuruGonderimLog
                            {
                                DuyuruId = id,
                                AliciEmail = email,
                                AliciKategorisi = "CC",
                                GonderimDurumu = sent ? "BASARILI" : "BASARISIZ",
                                HataMesaji = hataMesaji,
                                GonderimTarihi = gonderimTarihi
                            });
                        }

                        // BCC Recipients
                        foreach (var email in request.BccRecipients)
                        {
                            sendLogs.Add(new EpostaDuyuruGonderimLog
                            {
                                DuyuruId = id,
                                AliciEmail = email,
                                AliciKategorisi = "BCC",
                                GonderimDurumu = sent ? "BASARILI" : "BASARISIZ",
                                HataMesaji = hataMesaji,
                                GonderimTarihi = gonderimTarihi
                            });
                        }

                        // Batch insert logs
                        await _context.EpostaDuyuruGonderimLoglari.AddRangeAsync(sendLogs);

                        // ÖNEMLİ: EPOSTA_DUYURU_ALICILARI tablosunu da güncelle
                        foreach (var alici in finalAnnouncement.Alicilar)
                        {
                            alici.GonderimDurumu = gonderimDurumu;
                            alici.GonderimTarihi = gonderimTarihi;
                            if (!sent)
                            {
                                alici.HataMesaji = hataMesaji;
                            }
                        }

                        // Update announcement status
                        if (sent)
                        {
                            finalAnnouncement.Durum = DuyuruDurum.GONDERILDI;
                            finalAnnouncement.GercekGonderimTarihi = gonderimTarihi;
                            finalAnnouncement.ToplamAliciSayisi = sendLogs.Count;
                            finalAnnouncement.BasariliGonderimSayisi = sendLogs.Count;
                            finalAnnouncement.BasarisizGonderimSayisi = 0;
                            _logger.LogInformation("Announcement sent successfully: {Id} to {Count} recipients by user {UserId} from", id, sendLogs.Count, kullaniciId);

                            // Hareket kaydı: Başarılı gönderim (aynı transaction içinde)
                            AddHareket(id, DuyuruDurum.GONDERILIYOR, DuyuruDurum.GONDERILDI,
                                "GONDERIM", kullaniciId, $"Email başarıyla gönderildi. Toplam alıcı: {sendLogs.Count}, Başarılı: {sendLogs.Count}", null);

                            // Audit log: Başarılı gönderim
                            await _auditLog.LogAsync(
                                kategori: "EMAIL",
                                islem: "DUYURU_GONDERIM_BASARILI",
                                detay: $"Duyuru başarıyla gönderildi. ID: {id}, Konu: {finalAnnouncement.Konu}, Alıcı: {sendLogs.Count}, Başarılı: {sendLogs.Count}"
                            );
                        }
                        else
                        {
                            finalAnnouncement.Durum = DuyuruDurum.ONAYLANDI; // revert to approved state
                            finalAnnouncement.ToplamAliciSayisi = sendLogs.Count;
                            finalAnnouncement.BasariliGonderimSayisi = 0;
                            finalAnnouncement.BasarisizGonderimSayisi = sendLogs.Count;
                            _logger.LogError("Announcement send failed: {Id} to {Count} recipients by user {UserId} from ", id, sendLogs.Count, kullaniciId);

                            // Hareket kaydı: Başarısız gönderim (durum geri alındı) - aynı transaction içinde
                            AddHareket(id, DuyuruDurum.GONDERILIYOR, DuyuruDurum.ONAYLANDI,
                                "GONDERIM", kullaniciId, $"Email gönderimi başarısız oldu ve durum onaylandı'ya geri alındı. Hata: {hataMesaji}", null);

                            // Audit log: Başarısız gönderim
                            await _auditLog.LogAsync(
                                kategori: "EMAIL",
                                islem: "DUYURU_GONDERIM_HATASI",
                                detay: $"Duyuru gönderimi başarısız. ID: {id}, Konu: {finalAnnouncement.Konu}, Hata: {hataMesaji}",
                                logSeviye: "ERROR"
                            );
                        }

                        finalAnnouncement.GuncellemeTarihi = gonderimTarihi;
                        // Tek transaction: Duyuru güncelleme + Hareket kaydı
                        await _context.SaveChangesAsync();
                        await loggingTransaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    await loggingTransaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed while updating announcement status and logging recipients for {Id}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating announcement status and logging recipients for {Id}", id);
            }
        }
        catch (UnauthorizedAccessException uex)
        {
            // GÜVENLİK: Yetkisiz email gönderme girişimi
            _logger.LogError(uex, "SECURITY: Unauthorized email sending attempt for announcement {Id}", id);

            // Duyuru durumunu IPTAL olarak güncelle (güvenlik ihlali - sistemsel iptal)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE EPOSTA_DUYURULARI SET DURUM = {0} WHERE ID = {1}",
                DuyuruDurum.IPTAL, id);

            // Hareket kaydı: Sistemsel iptal (yetkisiz gönderici)
            AddHareket(id, DuyuruDurum.GONDERILIYOR, DuyuruDurum.IPTAL,
                "IPTAL", null, $"Sistem tarafından iptal edildi: Yetkisiz email gönderici adresi. Hata: {uex.Message}", null);
            await _context.SaveChangesAsync();

            // Security event kaydı
            await _auditLog.LogAsync(
                kategori: "SYSTEM",
                islem: "UNAUTHORIZED_EMAIL_SEND",
                detay: $"Yetkisiz email gönderme girişimi engellendi. Duyuru ID: {id}, Hata: {uex.Message}",
                kullaniciId: null,
                logSeviye: "ERROR"
            );
        }
        catch (InvalidOperationException iex)
        {
            // Geçersiz konfigürasyon
            _logger.LogError(iex, "Invalid configuration for announcement {Id}", id);

            // Duyuru durumunu IPTAL olarak güncelle (geçersiz konfigürasyon - sistemsel iptal)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE EPOSTA_DUYURULARI SET DURUM = {0} WHERE ID = {1}",
                DuyuruDurum.IPTAL, id);

            // Hareket kaydı: Sistemsel iptal (geçersiz konfigürasyon)
            AddHareket(id, DuyuruDurum.GONDERILIYOR, DuyuruDurum.IPTAL,
                "IPTAL", null, $"Sistem tarafından iptal edildi: Geçersiz konfigürasyon. Hata: {iex.Message}", null);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in send job for announcement {Id}", id);
            // Note: No need to revert status as we keep it ONAYLANDI throughout the process
        }
    }

    #region Helper Methods

    /// <summary>
    /// Hareket kaydı ekler. NOT: SaveChangesAsync çağırmaz, ana metodun transaction'ına dahil olur.
    /// </summary>
    private void AddHareket(int duyuruId, string? oncekiDurum, string yeniDurum, string islemTipi, int? kullaniciId, string? aciklama, int? secilenOnaylayiciId)
    {
        var hareket = new EpostaDuyuruHareket
        {
            DuyuruId = duyuruId,
            OncekiDurum = oncekiDurum,
            YeniDurum = yeniDurum,
            IslemTipi = islemTipi,
            KullaniciId = kullaniciId,
            Aciklama = aciklama,
            SecilenOnaylayiciId = secilenOnaylayiciId,
            IslemTarihi = DateTime.Now
        };

        _context.EpostaDuyuruHareketleri.Add(hareket);

        _logger.LogDebug("Hareket eklendi (commit bekliyor): Duyuru={DuyuruId}, {OncekiDurum} -> {YeniDurum}, İşlem={IslemTipi}",
            duyuruId, oncekiDurum ?? "NULL", yeniDurum, islemTipi);
    }

    private static void AddByCategory(string category, string email, HashSet<string> toSet, HashSet<string> ccSet, HashSet<string> bccSet)
    {
        switch ((category ?? "BCC").ToUpperInvariant())
        {
            case "TO":
                toSet.Add(email);
                break;

            case "CC":
                ccSet.Add(email);
                break;

            default:
                bccSet.Add(email);
                break;
        }
    }

    #endregion Helper Methods
}