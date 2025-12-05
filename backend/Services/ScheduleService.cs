using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface IScheduleService
{
    Task<ResponseDataModel<ScheduleResponse>> CreateScheduleAsync(CreateScheduleRequest request, int kullaniciId);

    Task<ResponseDataModel<List<ScheduleResponse>>> CreateBulkScheduleAsync(CreateBulkScheduleRequest request, int kullaniciId);

    Task<ResponseDataModel<List<ScheduleResponse>>> GetAllSchedulesAsync(int kullaniciId, string userRole, string? durum = null);

    Task<ResponseDataModel<List<ScheduleResponse>>> GetSchedulesForAnnouncementAsync(int duyuruId);

    Task<ResponseModel> CancelScheduleAsync(int scheduleId, int kullaniciId, string cancelNote);

    Task<ResponseModel> DeleteScheduleAsync(int scheduleId, int kullaniciId);

    Task ProcessScheduledAnnouncementAsync(int scheduleId);
}

public class ScheduleService : IScheduleService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<ScheduleService> _logger;
    private readonly IAuditLogService _auditLog;
    private readonly IAnnouncementApprovalService _approvalService;

    public ScheduleService(
        DeuEpostaContext context,
        ILogger<ScheduleService> logger,
        IAuditLogService auditLog,
        IAnnouncementApprovalService approvalService)
    {
        _context = context;
        _logger = logger;
        _auditLog = auditLog;
        _approvalService = approvalService;
    }

    public async Task<ResponseDataModel<ScheduleResponse>> CreateScheduleAsync(CreateScheduleRequest request, int kullaniciId)
    {
        try
        {
            // Duyuru kontrolü
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == request.DuyuruId);

            if (announcement == null)
                return ResponseDataModel<ScheduleResponse>.ErrorResult("Duyuru bulunamadı", 404);

            // Sadece ONAYLANDI durumundaki duyurular için zamanlama yapılabilir
            if (announcement.Durum != DuyuruDurum.ONAYLANDI)
                return ResponseDataModel<ScheduleResponse>.ErrorResult("Sadece onaylanmış duyurular için zamanlama yapılabilir", 400);

            // Alıcı kontrolü
            if (!announcement.Alicilar.Any())
                return ResponseDataModel<ScheduleResponse>.ErrorResult("Alıcı listesi boş, zamanlama yapılamaz", 400);

            // Tarih kontrolü - geçmişe zamanlama yapılamaz
            // Request UTC olarak gelir, onu local time'a çevir
            var scheduledTimeLocal = request.ZamanlanmaTarihi.ToLocalTime();
            if (scheduledTimeLocal <= DateTime.Now)
                return ResponseDataModel<ScheduleResponse>.ErrorResult("Geçmiş tarih için zamanlama yapılamaz", 400);

            // KRİTİK: Duplicate schedule kontrolü (concurrent creation önlemi)
            // Aynı duyuru için aktif zamanlama varsa yeni oluşturma
            var existingSchedule = await _context.EpostaDuyuruZamanlamalari
                .FirstOrDefaultAsync(z => z.DuyuruId == request.DuyuruId && z.Durum == "BEKLEMEDE");

            if (existingSchedule != null)
            {
                _logger.LogWarning("Duplicate schedule creation attempt for announcement {AnnouncementId}. Existing schedule: {ScheduleId}",
                    request.DuyuruId, existingSchedule.Id);
                return ResponseDataModel<ScheduleResponse>.ErrorResult(
                    $"Bu duyuru için zaten aktif bir zamanlama var (Zamanlama ID: {existingSchedule.Id}, Tarih: {existingSchedule.ZamanlanmaTarihi:dd.MM.yyyy HH:mm})",
                    400);
            }

            // Transaction ile atomik oluşturma (race condition önlemi)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Yeni zamanlama oluştur - database'e local time kaydet
                var schedule = new EpostaDuyuruZamanlama
                {
                    DuyuruId = request.DuyuruId,
                    ZamanlanmaTarihi = scheduledTimeLocal,
                    Durum = "BEKLEMEDE",
                    AliciSayisi = announcement.Alicilar.Count,
                    OlusturanKullaniciId = kullaniciId,
                    OlusturmaTarihi = DateTime.Now
                };

                // Önce database'e kaydet ki ID oluşsun
                _context.EpostaDuyuruZamanlamalari.Add(schedule);
                await _context.SaveChangesAsync();

                // Hangfire job oluştur - delay hesapla (transaction dışında)
                var jobId = BackgroundJob.Schedule<IScheduleService>(
                    s => s.ProcessScheduledAnnouncementAsync(schedule.Id),
                    scheduledTimeLocal - DateTime.Now
                );

                // Job ID'yi kaydet
                schedule.HangfireJobId = jobId;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Schedule created: {ScheduleId} for announcement {AnnouncementId} at {ScheduledTime}",
                    schedule.Id, request.DuyuruId, request.ZamanlanmaTarihi);

                // Audit log
                await _auditLog.LogAsync(
                    kategori: "EMAIL",
                    islem: "ZAMANLAMA_OLUSTURMA",
                    detay: $"Duyuru zamanlaması oluşturuldu. Duyuru ID: {request.DuyuruId}, Zamanlama ID: {schedule.Id}, Tarih: {scheduledTimeLocal:dd.MM.yyyy HH:mm}"
                );

                var response = new ScheduleResponse
                {
                    Id = schedule.Id,
                    DuyuruId = schedule.DuyuruId,
                    ZamanlanmaTarihi = schedule.ZamanlanmaTarihi,
                    Durum = schedule.Durum,
                    AliciSayisi = schedule.AliciSayisi,
                    HangfireJobId = schedule.HangfireJobId,
                    OlusturmaTarihi = schedule.OlusturmaTarihi
                };

                return ResponseDataModel<ScheduleResponse>.SuccessResult(response, "Zamanlama başarıyla oluşturuldu");
            }
            catch (Exception innerEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(innerEx, "Transaction error creating schedule for announcement {AnnouncementId}", request.DuyuruId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule for announcement {AnnouncementId}", request.DuyuruId);
            return ResponseDataModel<ScheduleResponse>.ErrorResult("Zamanlama oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ScheduleResponse>>> CreateBulkScheduleAsync(CreateBulkScheduleRequest request, int kullaniciId)
    {
        try
        {
            // Duyuru kontrolü
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == request.DuyuruId);

            if (announcement == null)
                return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Duyuru bulunamadı", 404);

            // Sadece ONAYLANDI durumundaki duyurular için zamanlama yapılabilir
            if (announcement.Durum != DuyuruDurum.ONAYLANDI)
                return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Sadece onaylanmış duyurular için zamanlama yapılabilir", 400);

            // Alıcı kontrolü
            if (!announcement.Alicilar.Any())
                return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Alıcı listesi boş, zamanlama yapılamaz", 400);

            // Tarih kontrolü - UTC'den local time'a çevir
            var startTimeLocal = request.BaslangicTarihi.ToLocalTime();
            var endTimeLocal = request.BitisTarihi.ToLocalTime();

            if (startTimeLocal <= DateTime.Now)
                return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Başlangıç tarihi geçmişte olamaz", 400);

            if (endTimeLocal <= startTimeLocal)
                return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Bitiş tarihi başlangıç tarihinden sonra olmalıdır", 400);

            // Zamanlamaları oluştur
            var schedules = new List<EpostaDuyuruZamanlama>();
            var currentDate = startTimeLocal;

            while (currentDate <= endTimeLocal)
            {
                var schedule = new EpostaDuyuruZamanlama
                {
                    DuyuruId = request.DuyuruId,
                    ZamanlanmaTarihi = currentDate,
                    Durum = "BEKLEMEDE",
                    AliciSayisi = announcement.Alicilar.Count,
                    OlusturanKullaniciId = kullaniciId,
                    OlusturmaTarihi = DateTime.Now
                };

                schedules.Add(schedule);
                currentDate = currentDate.AddDays(request.TekrarGunAraligi);
            }

            // Veritabanına ekle
            _context.EpostaDuyuruZamanlamalari.AddRange(schedules);
            await _context.SaveChangesAsync();

            // Hangfire job'larını oluştur
            foreach (var schedule in schedules)
            {
                var delay = schedule.ZamanlanmaTarihi - DateTime.Now;
                if (delay.TotalSeconds > 0)
                {
                    var jobId = BackgroundJob.Schedule<IScheduleService>(
                        s => s.ProcessScheduledAnnouncementAsync(schedule.Id),
                        delay
                    );
                    schedule.HangfireJobId = jobId;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk schedules created: {Count} schedules for announcement {AnnouncementId}",
                schedules.Count, request.DuyuruId);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "TOPLU_ZAMANLAMA_OLUSTURMA",
                detay: $"Toplu zamanlama oluşturuldu. Duyuru ID: {request.DuyuruId}, Zamanlama Sayısı: {schedules.Count}, Başlangıç: {startTimeLocal:dd.MM.yyyy HH:mm}, Bitiş: {endTimeLocal:dd.MM.yyyy HH:mm}, Aralık: {request.TekrarGunAraligi} gün"
            );

            var responses = schedules.Select(s => new ScheduleResponse
            {
                Id = s.Id,
                DuyuruId = s.DuyuruId,
                ZamanlanmaTarihi = s.ZamanlanmaTarihi,
                Durum = s.Durum,
                AliciSayisi = s.AliciSayisi,
                HangfireJobId = s.HangfireJobId,
                OlusturmaTarihi = s.OlusturmaTarihi
            }).ToList();

            return ResponseDataModel<List<ScheduleResponse>>.SuccessResult(responses, $"{schedules.Count} zamanlama başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk schedules for announcement {AnnouncementId}", request.DuyuruId);
            return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Toplu zamanlama oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ScheduleResponse>>> GetAllSchedulesAsync(int kullaniciId, string userRole, string? durum = null)
    {
        try
        {
            var query = _context.EpostaDuyuruZamanlamalari
                .Include(z => z.Duyuru)
                .AsQueryable();

            // Role-based filtreleme
            if (userRole == "EDITOR")
            {
                // Editor: Sadece kendi oluşturduğu duyuruların zamanlamalarını görsün
                // Zamanlama kaydını kim oluşturduğu önemli değil, duyurunun sahibi önemli
                query = query.Where(z => z.Duyuru != null && z.Duyuru.OlusturanKullaniciId == kullaniciId);
            }
            else if (userRole == "MANAGER")
            {
                // Manager: Kendi oluşturduğu + onayladığı/onaylayacağı duyuruların zamanlamalarını görsün
                query = query.Where(z => z.Duyuru != null && (
                    z.Duyuru.OlusturanKullaniciId == kullaniciId ||
                    z.Duyuru.SonOnaylayanKullaniciId == kullaniciId));
            }
            // ADMIN: Tümünü görebilir, filtreleme yok

            // Durum filtreleme
            if (!string.IsNullOrEmpty(durum))
            {
                query = query.Where(z => z.Durum == durum);
            }

            var schedules = await query
                .OrderByDescending(z => z.OlusturmaTarihi)
                .Select(z => new ScheduleResponse
                {
                    Id = z.Id,
                    DuyuruId = z.DuyuruId,
                    Konu = z.Duyuru != null ? z.Duyuru.Konu : "",
                    ZamanlanmaTarihi = z.ZamanlanmaTarihi,
                    Durum = z.Durum,
                    GonderimTarihi = z.GonderimTarihi,
                    AliciSayisi = z.AliciSayisi,
                    HangfireJobId = z.HangfireJobId,
                    HataMesaji = z.HataMesaji,
                    IptalNotu = z.IptalNotu,
                    OlusturmaTarihi = z.OlusturmaTarihi
                })
                .ToListAsync();

            return ResponseDataModel<List<ScheduleResponse>>.SuccessResult(schedules, "Zamanlamalar getirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all schedules");
            return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Zamanlamalar getirilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ScheduleResponse>>> GetSchedulesForAnnouncementAsync(int duyuruId)
    {
        try
        {
            var schedules = await _context.EpostaDuyuruZamanlamalari
                .Include(z => z.Duyuru)
                .Where(z => z.DuyuruId == duyuruId)
                .OrderBy(z => z.ZamanlanmaTarihi)
                .Select(z => new ScheduleResponse
                {
                    Id = z.Id,
                    DuyuruId = z.DuyuruId,
                    Konu = z.Duyuru != null ? z.Duyuru.Konu : "",
                    ZamanlanmaTarihi = z.ZamanlanmaTarihi,
                    Durum = z.Durum,
                    GonderimTarihi = z.GonderimTarihi,
                    AliciSayisi = z.AliciSayisi,
                    HangfireJobId = z.HangfireJobId,
                    HataMesaji = z.HataMesaji,
                    IptalNotu = z.IptalNotu,
                    OlusturmaTarihi = z.OlusturmaTarihi
                })
                .ToListAsync();

            return ResponseDataModel<List<ScheduleResponse>>.SuccessResult(schedules, "Zamanlamalar getirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedules for announcement {AnnouncementId}", duyuruId);
            return ResponseDataModel<List<ScheduleResponse>>.ErrorResult("Zamanlamalar getirilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> CancelScheduleAsync(int scheduleId, int kullaniciId, string cancelNote)
    {
        try
        {
            var schedule = await _context.EpostaDuyuruZamanlamalari.FirstOrDefaultAsync(z => z.Id == scheduleId);

            if (schedule == null)
                return ResponseModel.ErrorResult("Zamanlama bulunamadı", 404);

            // Sadece BEKLEMEDE durumundaki zamanlamalar iptal edilebilir
            if (schedule.Durum != "BEKLEMEDE")
                return ResponseModel.ErrorResult("Sadece beklemedeki zamanlamalar iptal edilebilir", 400);

            // Hangfire job'ı iptal et
            if (!string.IsNullOrEmpty(schedule.HangfireJobId))
            {
                try
                {
                    BackgroundJob.Delete(schedule.HangfireJobId);
                    _logger.LogInformation("Hangfire job {JobId} deleted for cancelled schedule {ScheduleId}",
                        schedule.HangfireJobId, scheduleId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete Hangfire job {JobId} for schedule {ScheduleId}",
                        schedule.HangfireJobId, scheduleId);
                }
            }

            schedule.Durum = "IPTAL";
            schedule.IptalNotu = cancelNote;
            schedule.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule cancelled: {ScheduleId} by user {UserId}", scheduleId, kullaniciId);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "ZAMANLAMA_IPTAL",
                detay: $"Zamanlama iptal edildi. Zamanlama ID: {scheduleId}, Duyuru ID: {schedule.DuyuruId}, İptal Notu: {cancelNote}"
            );

            return ResponseModel.SuccessResult("Zamanlama iptal edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling schedule {ScheduleId}", scheduleId);
            return ResponseModel.ErrorResult("Zamanlama iptal edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteScheduleAsync(int scheduleId, int kullaniciId)
    {
        try
        {
            var schedule = await _context.EpostaDuyuruZamanlamalari.FirstOrDefaultAsync(z => z.Id == scheduleId);

            if (schedule == null)
                return ResponseModel.ErrorResult("Zamanlama bulunamadı", 404);

            // Gönderilmiş veya gönderim başlamış zamanlamalar silinemez
            if (schedule.Durum == DuyuruDurum.GONDERILDI)
                return ResponseModel.ErrorResult("Gönderilmiş zamanlamalar silinemez", 400);

            // Hangfire job'ı iptal et
            if (!string.IsNullOrEmpty(schedule.HangfireJobId) && schedule.Durum == "BEKLEMEDE")
            {
                try
                {
                    BackgroundJob.Delete(schedule.HangfireJobId);
                    _logger.LogInformation("Hangfire job {JobId} deleted for deleted schedule {ScheduleId}",
                        schedule.HangfireJobId, scheduleId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete Hangfire job {JobId} for schedule {ScheduleId}",
                        schedule.HangfireJobId, scheduleId);
                }
            }

            _context.EpostaDuyuruZamanlamalari.Remove(schedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule deleted: {ScheduleId} by user {UserId}", scheduleId, kullaniciId);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "ZAMANLAMA_SILME",
                detay: $"Zamanlama silindi. Zamanlama ID: {scheduleId}, Duyuru ID: {schedule.DuyuruId}"
            );

            return ResponseModel.SuccessResult("Zamanlama silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule {ScheduleId}", scheduleId);
            return ResponseModel.ErrorResult("Zamanlama silinirken hata oluştu", 500);
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessScheduledAnnouncementAsync(int scheduleId)
    {
        _logger.LogInformation("🕐 Scheduled announcement job started for schedule {ScheduleId}", scheduleId);

        try
        {
            var schedule = await _context.EpostaDuyuruZamanlamalari
                .Include(z => z.Duyuru)
                    .ThenInclude(d => d!.Alicilar)
                .FirstOrDefaultAsync(z => z.Id == scheduleId);

            if (schedule == null)
            {
                _logger.LogError("Schedule not found: {ScheduleId}", scheduleId);
                return;
            }

            if (schedule.Durum != "BEKLEMEDE")
            {
                _logger.LogInformation("Schedule {ScheduleId} not in BEKLEMEDE state (current: {State})", scheduleId, schedule.Durum);
                return;
            }

            if (schedule.Duyuru == null)
            {
                _logger.LogError("Announcement not found for schedule {ScheduleId}", scheduleId);
                schedule.Durum = "HATA";
                schedule.HataMesaji = "Duyuru bulunamadı";
                schedule.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
                return;
            }

            // Duyuru ONAYLANDI durumunda değilse gönderme
            if (schedule.Duyuru.Durum != DuyuruDurum.ONAYLANDI)
            {
                _logger.LogWarning("Announcement {AnnouncementId} for schedule {ScheduleId} not in ONAYLANDI state (current: {State})",
                    schedule.DuyuruId, scheduleId, schedule.Duyuru.Durum);
                schedule.Durum = "HATA";
                schedule.HataMesaji = $"Duyuru uygun durumda değil: {schedule.Duyuru.Durum}";
                schedule.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
                return;
            }

            // ProcessSendAnnouncementJob'ı çağır (existing logic)
            await _approvalService.ProcessSendAnnouncementJob(schedule.DuyuruId, schedule.OlusturanKullaniciId);

            // Zamanlama durumunu güncelle
            schedule.Durum = "GONDERILDI";
            schedule.GonderimTarihi = DateTime.Now;
            schedule.GuncellemeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Scheduled announcement sent successfully for schedule {ScheduleId}", scheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled announcement for schedule {ScheduleId}", scheduleId);

            // Hata durumunu kaydet
            try
            {
                var schedule = await _context.EpostaDuyuruZamanlamalari.FirstOrDefaultAsync(z => z.Id == scheduleId);
                if (schedule != null)
                {
                    schedule.Durum = "HATA";
                    schedule.HataMesaji = ex.Message;
                    schedule.GuncellemeTarihi = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update schedule {ScheduleId} status after error", scheduleId);
            }

            throw; // Re-throw for Hangfire retry
        }
    }
}