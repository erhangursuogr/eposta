using AutoMapper;
using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

/// <summary>
/// Duyuru onay workflow işlemlerini yönetir
/// </summary>
public interface IAnnouncementApprovalWorkflowService
{
    Task<ResponseModel> SubmitForApprovalAsync(int id, int kullaniciId);

    Task<ResponseModel> CoordinatorApproveAsync(int id, int koordinatorId, int managerId, string? note = null);

    Task<ResponseModel> CoordinatorRejectAsync(int id, int koordinatorId, string reason);

    Task<ResponseModel> ManagerApproveAsync(int id, int managerId, string? note = null);

    Task<ResponseModel> ManagerRejectAsync(int id, int managerId, string reason);

    Task<ResponseModel> ManagerApproveAndSendAsync(int id, int managerId, string? note = null);

    Task<ResponseModel> ApproveAnnouncementAsync(int id, int kullaniciId, string? note = null);

    Task<ResponseModel> RejectAnnouncementAsync(int id, int kullaniciId, string reason);

    Task<ResponseModel> CancelAnnouncementAsync(int id, int kullaniciId, string reason);
}

public class AnnouncementApprovalWorkflowService : IAnnouncementApprovalWorkflowService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AnnouncementApprovalService> _logger;
    private readonly IAnnouncementApprovalService _announcementApprovalService;
    private readonly IAnnouncementApprovalNotificationService _announcementNotificationService;
    private readonly IAuditLogService _auditLog;

    public AnnouncementApprovalWorkflowService(
        DeuEpostaContext context,
        ILogger<AnnouncementApprovalService> logger,        
        IAnnouncementApprovalService announcementApprovalService,
        IAnnouncementApprovalNotificationService announcementNotificationService,        
        IAuditLogService auditLog
        )
    {
        _context = context;
        _logger = logger;
        _announcementApprovalService = announcementApprovalService;
        _announcementNotificationService = announcementNotificationService;
        _auditLog = auditLog;
    }

    /// <summary>
    /// ADMIN/MANAGER için: Duyuruyu onayla ve direkt gönder
    /// Onay süreci atlanır, bildirim emaili gönderilmez
    /// </summary>

    public async Task<ResponseModel> SubmitForApprovalAsync(int id, int kullaniciId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.OlusturanKullaniciId != kullaniciId)
                return ResponseModel.ErrorResult("Bu duyuruyu onaya gönderme yetkiniz yok", 403);

            // STATE MACHINE FIX: State machine validation kullan
            if (!announcement.CanTransitionTo(DuyuruDurum.ILK_ONAY_BEKLIYOR))
            {
                var allowedTransitions = DuyuruDurum.GetAllowedTransitions(announcement.Durum);
                var allowedStates = string.Join(", ", allowedTransitions);
                return ResponseModel.ErrorResult(
                    $"'{announcement.Durum}' durumundan onaya gönderilemez. İzin verilen geçişler: {allowedStates}", 400);
            }

            // EMAIL tipinde ise alıcı kontrolü - en az 1 alıcı olmalı
            if (announcement.IsEmailType() && !announcement.Alicilar.Any())
                return ResponseModel.ErrorResult("Email duyurusu için alıcı listesi boş olamaz", 400);

            var oncekiDurum = announcement.Durum;
            announcement.Durum = DuyuruDurum.ILK_ONAY_BEKLIYOR;
            announcement.IlkOnaylayanKullaniciId = null; // İlk onayda henüz onaylayan yok
            announcement.SonOnaylayanKullaniciId = null; // Son onaylayan henüz yok
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı: ONAYA_GONDERME (aynı transaction içinde)
            var aciklama = oncekiDurum == DuyuruDurum.REDDEDILDI
                ? "Düzeltmeler yapıldı, duyuru tekrar onaya gönderildi"
                : "Duyuru ilk onay için gönderildi";
            AddHareket(announcement.Id, oncekiDurum, DuyuruDurum.ILK_ONAY_BEKLIYOR,
                "ONAYA_GONDERME", kullaniciId, aciklama, null);

            // Tek transaction: Duyuru güncelleme + Hareket kaydı
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement submitted for approval: {Id} by user {UserId} from", id, kullaniciId);

            // Audit log: Onaya gönderme
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "ONAYA_GONDERME",
                detay: $"Duyuru onaya gönderildi. ID: {id}, Konu: {announcement.Konu}"
            );

            // Onaylayıcıya bildirim email'i gönder
            await _announcementNotificationService.SendSubmittedForApprovalNotificationAsync(announcement, kullaniciId);

            return ResponseModel.SuccessResult("Duyuru onaya gönderildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting announcement {Id} for approval", id);
            return ResponseModel.ErrorResult("Duyuru onaya gönderilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ApproveAnnouncementAsync(int id, int kullaniciId, string? note = null)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // STATE MACHINE FIX: Hardcoded kontrolü kaldırıp state machine validation kullan
            // Onay işlemi için önce SON_ONAY_BEKLIYOR'a geçiş olup olmadığını kontrol et
            var targetState = announcement.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR
                ? DuyuruDurum.SON_ONAY_BEKLIYOR
                : DuyuruDurum.ONAYLANDI;

            if (!announcement.CanTransitionTo(targetState))
            {
                var allowedTransitions = DuyuruDurum.GetAllowedTransitions(announcement.Durum);
                var allowedStates = string.Join(", ", allowedTransitions);
                return ResponseModel.ErrorResult(
                    $"'{announcement.Durum}' durumundan onaylama yapılamaz. İzin verilen geçişler: {allowedStates}", 400);
            }

            // Alıcı kontrolü - en az 1 alıcı olmalı
            if (!announcement.Alicilar.Any())
                return ResponseModel.ErrorResult("Alıcı listesi boş, duyuru onaylanamaz", 400);

            // Kendi duyurusunu onaylama engeli (ADMIN hariç)
            var approver = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == kullaniciId);

            var isAdmin = approver?.Rol?.RolKodu == "ADMIN";

            if (!isAdmin && announcement.OlusturanKullaniciId == kullaniciId)
            {
                return ResponseModel.ErrorResult("Kendi oluşturduğunuz duyuruyu onaylayamazsınız", 403);
            }

            // Kendisine atanan duyuru kontrolü (ADMIN hariç)
            if (!isAdmin &&
                announcement.SonOnaylayanKullaniciId.HasValue &&
                announcement.SonOnaylayanKullaniciId.Value != kullaniciId)
            {
                return ResponseModel.ErrorResult("Bu duyuru başka bir yöneticiye atanmış", 403);
            }

            var oncekiDurum = announcement.Durum;

            // STATE MACHINE FIX: İki aşamalı onay sistemini doğru uygula
            string yeniDurum;
            if (oncekiDurum == DuyuruDurum.ILK_ONAY_BEKLIYOR)
            {
                // İlk onay: COORDINATOR → SON_ONAY_BEKLIYOR (Manager onayına gönder)
                yeniDurum = DuyuruDurum.SON_ONAY_BEKLIYOR;
                announcement.IlkOnaylayanKullaniciId = kullaniciId;
            }
            else // SON_ONAY_BEKLIYOR
            {
                // Son onay: MANAGER → ONAYLANDI
                yeniDurum = DuyuruDurum.ONAYLANDI;
                announcement.SonOnaylayanKullaniciId = kullaniciId;
            }

            announcement.Durum = yeniDurum;
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı ekle
            _context.EpostaDuyuruHareketleri.Add(new EpostaDuyuruHareket
            {
                DuyuruId = id,
                OncekiDurum = oncekiDurum,
                YeniDurum = yeniDurum,
                IslemTipi = "ONAYLAMA",
                KullaniciId = kullaniciId,
                Aciklama = note,
                IslemTarihi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement approved: {Id} by user {UserId} from", id, kullaniciId);

            // Audit log: Onaylama
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_ONAYLAMA",
                detay: $"Duyuru onaylandı. ID: {id}, Konu: {announcement.Konu}, Not: {note ?? "Yok"}"
            );

            // Oluşturan kullanıcıya onay bildirimi gönder
            await _announcementNotificationService.SendApprovedNotificationAsync(announcement, kullaniciId, note);

            return ResponseModel.SuccessResult("Duyuru onaylandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru onaylanırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> RejectAnnouncementAsync(int id, int kullaniciId, string reason)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // STATE MACHINE FIX: State machine validation kullan
            if (!announcement.CanTransitionTo(DuyuruDurum.REDDEDILDI))
            {
                var allowedTransitions = DuyuruDurum.GetAllowedTransitions(announcement.Durum);
                var allowedStates = string.Join(", ", allowedTransitions);
                return ResponseModel.ErrorResult(
                    $"'{announcement.Durum}' durumundan reddetme yapılamaz. İzin verilen geçişler: {allowedStates}", 400);
            }

            var oncekiDurum = announcement.Durum;
            announcement.Durum = DuyuruDurum.REDDEDILDI;
            // Legacy: Eğer ILK_ONAY_BEKLIYOR ise IlkOnaylayan, eğer SON_ONAY_BEKLIYOR ise SonOnaylayan set et
            if (oncekiDurum == DuyuruDurum.ILK_ONAY_BEKLIYOR)
                announcement.IlkOnaylayanKullaniciId = kullaniciId;
            else if (oncekiDurum == DuyuruDurum.SON_ONAY_BEKLIYOR)
                announcement.SonOnaylayanKullaniciId = kullaniciId;
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı ekle
            _context.EpostaDuyuruHareketleri.Add(new EpostaDuyuruHareket
            {
                DuyuruId = id,
                OncekiDurum = oncekiDurum,
                YeniDurum = DuyuruDurum.REDDEDILDI,
                IslemTipi = "REDDETME",
                KullaniciId = kullaniciId,
                Aciklama = reason,
                IslemTarihi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement rejected: {Id} by user {UserId} from, reason: {Reason}", id, kullaniciId, reason);

            // Audit log: Reddetme
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_REDDETME",
                detay: $"Duyuru reddedildi. ID: {id}, Konu: {announcement.Konu}, Red Nedeni: {reason}"
            );

            // Oluşturan kullanıcıya reddetme bildirimi gönder
            await _announcementNotificationService.SendRejectedNotificationAsync(announcement, kullaniciId, reason);

            return ResponseModel.SuccessResult("Duyuru reddedildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru reddedilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> CancelAnnouncementAsync(int id, int kullaniciId, string reason)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // STATE MACHINE FIX: State machine validation kullan
            if (!announcement.CanTransitionTo(DuyuruDurum.IPTAL))
            {
                var allowedTransitions = DuyuruDurum.GetAllowedTransitions(announcement.Durum);
                var allowedStates = string.Join(", ", allowedTransitions);
                return ResponseModel.ErrorResult(
                    $"'{announcement.Durum}' durumundan iptal yapılamaz. İzin verilen geçişler: {allowedStates}", 400);
            }

            // Zamanlamaları iptal et (yeni sistem)
            var pendingSchedules = await _context.EpostaDuyuruZamanlamalari
                .Where(z => z.DuyuruId == id && z.Durum == "BEKLEMEDE")
                .ToListAsync();

            // ÖNCE Hangfire job'ları sil (transaction dışında, external service)
            var failedJobDeletions = new List<int>();
            foreach (var schedule in pendingSchedules)
            {
                if (string.IsNullOrEmpty(schedule.HangfireJobId))
                    continue;

                bool deleted = false;
                Exception? lastException = null;

                // Retry mekanizması: 3 kez dene (geçici hatalar için)
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        // Önce job'ın var olup olmadığını kontrol et (false positive önleme)
                        var monitoringApi = JobStorage.Current.GetMonitoringApi();
                        var jobDetails = monitoringApi.JobDetails(schedule.HangfireJobId);

                        if (jobDetails == null)
                        {
                            // Job zaten silinmiş veya yok, sorun değil
                            _logger.LogWarning("Hangfire job {JobId} not found (already deleted), marking schedule {ScheduleId} as safe to cancel",
                                schedule.HangfireJobId, schedule.Id);
                            deleted = true;
                            break;
                        }

                        // Job mevcut, sil
                        BackgroundJob.Delete(schedule.HangfireJobId);
                        _logger.LogInformation("Hangfire job {JobId} deleted for cancelled schedule {ScheduleId} (attempt {Attempt})",
                            schedule.HangfireJobId, schedule.Id, attempt);
                        deleted = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId} (attempt {Attempt}/3)", schedule.HangfireJobId, attempt);

                        if (attempt < 3)
                        {
                            // Son denemeden önce bekle (geçici lock/network hatası için)
                            await Task.Delay(500 * attempt); // 500ms, 1000ms, 1500ms
                        }
                    }
                }

                if (!deleted)
                {
                    _logger.LogError(lastException, "CRITICAL: Could not delete Hangfire job {JobId} for schedule {ScheduleId} after 3 attempts. Job may still execute!",
                        schedule.HangfireJobId, schedule.Id);
                    failedJobDeletions.Add(schedule.Id);
                }
            }

            // KRİTİK: Eğer job silme başarısız olursa duyuruyu IPTAL ETME!
            // Job tetiklenirse yine gönderim yapabilir - kullanıcıya hata döndür
            if (failedJobDeletions.Any())
            {
                // Sadece zamanlama HATA durumunu kaydet (transaction ile)
                using var errorTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var schedule in pendingSchedules.Where(s => failedJobDeletions.Contains(s.Id)))
                    {
                        schedule.Durum = "HATA";
                        schedule.IptalNotu = $"Hangfire job silinemedi - Manuel kontrol gerekli!";
                        schedule.GuncellemeTarihi = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    await errorTransaction.CommitAsync();
                }
                catch
                {
                    await errorTransaction.RollbackAsync();
                    throw;
                }

                _logger.LogError("CRITICAL: Cannot cancel announcement {Id} - {Count} scheduled jobs could not be deleted after retry attempts: {ScheduleIds}",
                    id, failedJobDeletions.Count, string.Join(", ", failedJobDeletions));

                return ResponseModel.ErrorResult(
                    $"Duyuru iptal edilemedi! {failedJobDeletions.Count} zamanlanmış gönderim 3 denemeden sonra silinemedi. " +
                    $"Bu gönderimlerin zamanı geldiğinde tetiklenebilir. Lütfen sistem yöneticisiyle iletişime geçin. " +
                    $"Zamanlama ID'leri: {string.Join(", ", failedJobDeletions)}",
                    500);
            }

            // Job silme başarılı, şimdi duyuruyu ve zamanlamaları iptal et (transaction ile atomik)
            using var cancelTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var oncekiDurum = announcement.Durum;
                announcement.Durum = DuyuruDurum.IPTAL;
                announcement.GuncellemeTarihi = DateTime.Now;

                // Zamanlamaları IPTAL durumuna çevir
                foreach (var schedule in pendingSchedules)
                {
                    schedule.Durum = "IPTAL";
                    schedule.IptalNotu = $"Duyuru iptal edildi: {reason}";
                    schedule.GuncellemeTarihi = DateTime.Now;
                }

                // Hareket kaydı ekle
                _context.EpostaDuyuruHareketleri.Add(new EpostaDuyuruHareket
                {
                    DuyuruId = id,
                    OncekiDurum = oncekiDurum,
                    YeniDurum = DuyuruDurum.IPTAL,
                    IslemTipi = "IPTAL",
                    KullaniciId = kullaniciId,
                    Aciklama = reason,
                    IslemTarihi = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await cancelTransaction.CommitAsync();
            }
            catch
            {
                await cancelTransaction.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Announcement cancelled: {Id} by user {UserId} from, reason: {Reason}", id, kullaniciId, reason);

            // Audit log: İptal
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_IPTAL",
                detay: $"Duyuru iptal edildi. ID: {id}, Konu: {announcement.Konu}, İptal Nedeni: {reason}"
            );

            // İptal bildirim e-postası gönder
            await _announcementNotificationService.SendCancelledNotificationAsync(announcement, kullaniciId, reason);

            return ResponseModel.SuccessResult("Duyuru iptal edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru iptal edilirken hata oluştu", 500);
        }
    }

    #region İki Aşamalı Onay Sistemi - Kontrolör

    /// <summary>
    /// Kontrolör onayı - ILK_ONAY_BEKLIYOR → SON_ONAY_BEKLIYOR + Manager seçimi
    /// </summary>
    public async Task<ResponseModel> CoordinatorApproveAsync(int id, int koordinatorId, int managerId, string? note = null)
    {
        try
        {
            // Kontrolör yetkisini kontrol et
            var koordinator = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == koordinatorId);

            if (koordinator == null || !RolKodu.CanFirstApprove(koordinator.Rol?.RolKodu))
                return ResponseModel.ErrorResult("Kontrolör onay yetkisi bulunamadı", 403);

            // GÜVENLİK: Koordinatörün aktif olduğunu kontrol et
            if (koordinator.Aktif != "Y")
            {
                _logger.LogWarning("Inactive coordinator {KoordinatorId} attempted to approve announcement {AnnouncementId}", koordinatorId, id);
                return ResponseModel.ErrorResult("Hesabınız deaktif durumda. Sistem yöneticisiyle iletişime geçin.", 403);
            }

            // Manager yetkisini kontrol et
            var manager = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == managerId);

            if (manager == null || !RolKodu.CanFinalApprove(manager.Rol?.RolKodu))
                return ResponseModel.ErrorResult("Seçilen kullanıcı manager rolüne sahip değil", 400);

            // GÜVENLİK: Seçilen manager'ın aktif olduğunu kontrol et
            if (manager.Aktif != "Y")
            {
                _logger.LogWarning("Attempted to assign inactive manager {ManagerId} to announcement {AnnouncementId}", managerId, id);
                return ResponseModel.ErrorResult("Seçilen onaylayıcı deaktif durumda. Farklı bir onaylayıcı seçin.", 400);
            }

            // KRİTİK: Transaction ile atomik durum güncellemesi (race condition önlemi)
            // İki koordinatör aynı anda onaylayamaz
            using var transaction = await _context.Database.BeginTransactionAsync();

            var rowsUpdated = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE EPOSTA_DUYURULARI SET DURUM = {0}, ILK_ONAYLAYAN_KULLANICI_ID = {1}, SON_ONAYLAYAN_KULLANICI_ID = {2}, GUNCELLEME_TARIHI = {3} WHERE ID = {4} AND DURUM = {5}",
                DuyuruDurum.SON_ONAY_BEKLIYOR, koordinatorId, managerId, DateTime.Now, id, DuyuruDurum.ILK_ONAY_BEKLIYOR);

            if (rowsUpdated == 0)
            {
                _logger.LogWarning("Coordinator approval failed: Announcement {Id} not in ILK_ONAY_BEKLIYOR state or already approved", id);
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Duyuru zaten onaylanmış veya durumu uygun değil", 400);
            }

            // Re-fetch announcement with updated state
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .Include(d => d.OlusturanKullanici)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
            {
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);
            }

            // EMAIL tipinde ise alıcı kontrolü
            if (announcement.IsEmailType() && !announcement.Alicilar.Any())
            {
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Alıcı listesi boş, duyuru onaylanamaz", 400);
            }

            // Hareket kaydı: ONAYLAMA (Kontrolör onayı + Manager seçimi) - transaction içinde
            AddHareket(announcement.Id, DuyuruDurum.ILK_ONAY_BEKLIYOR, DuyuruDurum.SON_ONAY_BEKLIYOR,
                "ONAYLAMA", koordinatorId, note ?? "Kontrolör onayladı ve manager seçti", managerId);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Coordinator approved announcement {Id}, selected manager {ManagerId}", id, managerId);

            // Audit log
            await _auditLog.LogAsync("EMAIL", "KOORDINATOR_ONAY",
                $"Coordinator {koordinatorId} approved announcement {id}, selected manager {managerId}", koordinatorId);

            // Manager'a bilgilendirme maili gönder
            await _announcementNotificationService.SendSubmittedForApprovalNotificationAsync(announcement, koordinatorId);

            // Oluşturucuya onay bilgilendirme maili gönder
            await _announcementNotificationService.SendApprovalNotificationEmailAsync(announcement, koordinator.AdSoyad, "Kontrolör", note);

            return ResponseModel.SuccessResult("Duyuru başarıyla onaylandı ve manager'a gönderildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in coordinator approval for announcement {Id}", id);
            return ResponseModel.ErrorResult("Onaylama sırasında hata oluştu");
        }
    }

    /// <summary>
    /// Kontrolör reddi - ILK_ONAY_BEKLIYOR → TASLAK (otomatik)
    /// </summary>
    public async Task<ResponseModel> CoordinatorRejectAsync(int id, int koordinatorId, string reason)
    {
        try
        {
            // Kontrolör yetkisini kontrol et
            var koordinator = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == koordinatorId);

            if (koordinator == null || !RolKodu.CanFirstApprove(koordinator.Rol?.RolKodu))
                return ResponseModel.ErrorResult("Kontrolör reddetme yetkisi bulunamadı", 403);

            // GÜVENLİK: Koordinatörün aktif olduğunu kontrol et
            if (koordinator.Aktif != "Y")
            {
                _logger.LogWarning("Inactive coordinator {KoordinatorId} attempted to reject announcement {AnnouncementId}", koordinatorId, id);
                return ResponseModel.ErrorResult("Hesabınız deaktif durumda. Sistem yöneticisiyle iletişime geçin.", 403);
            }

            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.OlusturanKullanici)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.ILK_ONAY_BEKLIYOR)
                return ResponseModel.ErrorResult("Sadece ilk onay bekleyen duyurular reddedilebilir", 400);

            var oncekiDurum = announcement.Durum;
            announcement.Durum = DuyuruDurum.TASLAK; // Otomatik TASLAK'a dön
            announcement.IlkOnaylayanKullaniciId = null;
            announcement.SonOnaylayanKullaniciId = null;
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı: REDDETME (aynı transaction içinde)
            AddHareket(announcement.Id, oncekiDurum, DuyuruDurum.TASLAK,
                "REDDETME", koordinatorId, reason, null);

            // Tek transaction: Duyuru güncelleme + Hareket kaydı
            await _context.SaveChangesAsync();

            _logger.LogInformation("Coordinator rejected announcement {Id}, reason: {Reason}", id, reason);

            // Audit log
            await _auditLog.LogAsync("EMAIL", "KOORDINATOR_REDD",
                $"Coordinator {koordinatorId} rejected announcement {id}: {reason}", koordinatorId);

            // Oluşturucuya red bilgilendirme maili gönder
            await _announcementNotificationService.SendRejectionNotificationEmailAsync(announcement, koordinator.AdSoyad, reason, "Kontrolör");

            return ResponseModel.SuccessResult("Duyuru reddedildi ve taslak durumuna döndürüldü");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in coordinator rejection for announcement {Id}", id);
            return ResponseModel.ErrorResult("Reddetme sırasında hata oluştu");
        }
    }

    #endregion İki Aşamalı Onay Sistemi - Kontrolör

    #region İki Aşamalı Onay Sistemi - Manager

    /// <summary>
    /// Manager onayı - SON_ONAY_BEKLIYOR → ONAYLANDI
    /// </summary>
    public async Task<ResponseModel> ManagerApproveAsync(int id, int managerId, string? note = null)
    {
        try
        {
            // Manager yetkisini kontrol et
            var manager = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == managerId);

            if (manager == null || !RolKodu.CanFinalApprove(manager.Rol?.RolKodu))
                return ResponseModel.ErrorResult("Manager onay yetkisi bulunamadı", 403);

            // GÜVENLİK: Manager'ın aktif olduğunu kontrol et
            if (manager.Aktif != "Y")
            {
                _logger.LogWarning("Inactive manager {ManagerId} attempted to approve announcement {AnnouncementId}", managerId, id);
                return ResponseModel.ErrorResult("Hesabınız deaktif durumda. Sistem yöneticisiyle iletişime geçin.", 403);
            }

            // KRİTİK: Transaction ile atomik durum güncellemesi (race condition önlemi)
            // İki manager aynı anda onaylayamaz
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Manager kontrolü için önce duyuruyu oku
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .Include(d => d.OlusturanKullanici)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
            {
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);
            }

            // Manager kontrolü - kendisine atanmış mı?
            if (announcement.SonOnaylayanKullaniciId != managerId)
            {
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Bu duyuru size atanmamış", 403);
            }

            // EMAIL tipinde ise alıcı kontrolü
            if (announcement.IsEmailType() && !announcement.Alicilar.Any())
            {
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Alıcı listesi boş, duyuru onaylanamaz", 400);
            }

            // Atomik UPDATE: Sadece SON_ONAY_BEKLIYOR durumunda onaylayabilir
            var rowsUpdated = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE EPOSTA_DUYURULARI SET DURUM = {0}, GUNCELLEME_TARIHI = {1} WHERE ID = {2} AND DURUM = {3}",
                DuyuruDurum.ONAYLANDI, DateTime.Now, id, DuyuruDurum.SON_ONAY_BEKLIYOR);

            if (rowsUpdated == 0)
            {
                _logger.LogWarning("Manager approval failed: Announcement {Id} not in SON_ONAY_BEKLIYOR state or already approved", id);
                await transaction.RollbackAsync();
                return ResponseModel.ErrorResult("Duyuru zaten onaylanmış veya durumu uygun değil", 400);
            }

            // Hareket kaydı: ONAYLAMA (Manager final onayı) - transaction içinde
            AddHareket(announcement.Id, DuyuruDurum.SON_ONAY_BEKLIYOR, DuyuruDurum.ONAYLANDI,
                "ONAYLAMA", managerId, note ?? "Manager final onayı verdi", null);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Manager approved announcement {Id}", id);

            // Audit log
            await _auditLog.LogAsync("EMAIL", "YONETICI_ONAY",
                $"Manager {managerId} approved announcement {id}", managerId);

            // Oluşturucuya onay bilgilendirme maili gönder
            await _announcementNotificationService.SendApprovalNotificationEmailAsync(announcement, manager.AdSoyad, "Yönetici", note);

            return ResponseModel.SuccessResult("Duyuru başarıyla onaylandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manager approval for announcement {Id}", id);
            return ResponseModel.ErrorResult("Onaylama sırasında hata oluştu");
        }
    }

    /// <summary>
    /// Manager reddi - SON_ONAY_BEKLIYOR → TASLAK (otomatik)
    /// </summary>
    public async Task<ResponseModel> ManagerRejectAsync(int id, int managerId, string reason)
    {
        try
        {
            // Manager yetkisini kontrol et
            var manager = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == managerId);

            if (manager == null || !RolKodu.CanFinalApprove(manager.Rol?.RolKodu))
                return ResponseModel.ErrorResult("Manager reddetme yetkisi bulunamadı", 403);

            // GÜVENLİK: Manager'ın aktif olduğunu kontrol et
            if (manager.Aktif != "Y")
            {
                _logger.LogWarning("Inactive manager {ManagerId} attempted to reject announcement {AnnouncementId}", managerId, id);
                return ResponseModel.ErrorResult("Hesabınız deaktif durumda. Sistem yöneticisiyle iletişime geçin.", 403);
            }

            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.OlusturanKullanici)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.SON_ONAY_BEKLIYOR)
                return ResponseModel.ErrorResult("Sadece son onay bekleyen duyurular reddedilebilir", 400);

            // Manager kontrolü - kendisine atanmış mı?
            if (announcement.SonOnaylayanKullaniciId != managerId)
                return ResponseModel.ErrorResult("Bu duyuru size atanmamış", 403);

            var oncekiDurum = announcement.Durum;
            announcement.Durum = DuyuruDurum.TASLAK; // Otomatik TASLAK'a dön
            announcement.IlkOnaylayanKullaniciId = null;
            announcement.SonOnaylayanKullaniciId = null;
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı: REDDETME (aynı transaction içinde)
            AddHareket(announcement.Id, oncekiDurum, DuyuruDurum.TASLAK,
                "REDDETME", managerId, reason, null);

            // Tek transaction: Duyuru güncelleme + Hareket kaydı
            await _context.SaveChangesAsync();

            _logger.LogInformation("Manager rejected announcement {Id}, reason: {Reason}", id, reason);

            // Audit log
            await _auditLog.LogAsync("EMAIL", "YONETICI_REDD",
                $"Manager {managerId} rejected announcement {id}: {reason}", managerId);

            // Oluşturucuya red bilgilendirme maili gönder
            await _announcementNotificationService.SendRejectionNotificationEmailAsync(announcement, manager.AdSoyad, reason, "Yönetici");

            return ResponseModel.SuccessResult("Duyuru reddedildi ve taslak durumuna döndürüldü");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manager rejection for announcement {Id}", id);
            return ResponseModel.ErrorResult("Reddetme sırasında hata oluştu");
        }
    }

    /// <summary>
    /// Manager onayı ve direkt gönderim - SON_ONAY_BEKLIYOR → ONAYLANDI → GONDERILDI
    /// </summary>
    public async Task<ResponseModel> ManagerApproveAndSendAsync(int id, int managerId, string? note = null)
    {
        try
        {
            // Manager yetkisini kontrol et
            var manager = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == managerId);

            if (manager == null || !RolKodu.CanFinalApprove(manager.Rol?.RolKodu))
                return ResponseModel.ErrorResult("Manager onay yetkisi bulunamadı", 403);

            // GÜVENLİK: Manager'ın aktif olduğunu kontrol et
            if (manager.Aktif != "Y")
            {
                _logger.LogWarning("Inactive manager {ManagerId} attempted to approve and send announcement {AnnouncementId}", managerId, id);
                return ResponseModel.ErrorResult("Hesabınız deaktif durumda. Sistem yöneticisiyle iletişime geçin.", 403);
            }

            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.SON_ONAY_BEKLIYOR)
                return ResponseModel.ErrorResult("Sadece son onay bekleyen duyurular onaylanabilir", 400);

            // Manager kontrolü - kendisine atanmış mı?
            if (announcement.SonOnaylayanKullaniciId != managerId)
                return ResponseModel.ErrorResult("Bu duyuru size atanmamış", 403);

            // EMAIL tipinde olmalı (sosyal medya gönderilemez)
            if (!announcement.IsEmailType())
                return ResponseModel.ErrorResult("Sadece email duyuruları gönderilebilir", 400);

            // Alıcı kontrolü
            if (!announcement.Alicilar.Any())
                return ResponseModel.ErrorResult("Alıcı listesi boş, duyuru gönderilemez", 400);

            var oncekiDurum = announcement.Durum;
            announcement.Durum = DuyuruDurum.ONAYLANDI;
            // SonOnaylayanKullaniciId zaten set edilmiş (koordinatör tarafından), değiştirme
            announcement.GuncellemeTarihi = DateTime.Now;

            // Hareket kaydı: ONAYLAMA (aynı transaction içinde)
            AddHareket(announcement.Id, oncekiDurum, DuyuruDurum.ONAYLANDI,
                "ONAYLAMA", managerId, note ?? "Manager onayladı ve gönderime aldı", null);

            // Tek transaction: Duyuru güncelleme + Hareket kaydı
            await _context.SaveChangesAsync();

            _logger.LogInformation("Manager approved and queued announcement {Id} for sending", id);

            // Audit log
            await _auditLog.LogAsync("EMAIL", "YONETICI_ONAY_GONDER",
                $"Manager {managerId} approved and queued announcement {id} for sending", managerId);

            // Gönderim job'ı başlat (background)
            BackgroundJob.Enqueue(() => _announcementApprovalService.ProcessSendAnnouncementJob(id, managerId));

            return ResponseModel.SuccessResult("Duyuru onaylandı ve gönderime alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manager approve and send for announcement {Id}", id);
            return ResponseModel.ErrorResult("Onaylama ve gönderme sırasında hata oluştu");
        }
    }

    #endregion İki Aşamalı Onay Sistemi - Manager

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
}