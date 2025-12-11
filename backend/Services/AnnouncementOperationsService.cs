using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

/// <summary>
/// REFACTORING: Duyuru işlem servisi (Durum değişikliği, istatistik, log, test email, preview vb.)
/// CRUD işlemleri AnnouncementService'te kaldı
/// </summary>
public interface IAnnouncementOperationsService
{
    Task<ResponseModel> ChangeStatusAsync(int id, string yeniDurum, int kullaniciId, string? note = null);

    Task<ResponseDataModel<AnnouncementStatistics>> GetAnnouncementStatisticsAsync(int? kullaniciId = null);

    Task<ResponseDataModel<List<EpostaDuyuruGonderimLog>>> GetSendLogsAsync(int announcementId);

    Task<ResponseModel> ReactivateAnnouncementAsync(int id, int kullaniciId);

    Task<ResponseModel> SendTestEmailAsync(int id, int kullaniciId, string? testEmail);

    Task<ResponseDataModel<AnnouncementPreviewDto>> GetAnnouncementPreviewAsync(int announcementId);

    Task<ResponseDataModel<List<AnnouncementMovementView>>> GetAnnouncementMovementsAsync(int announcementId);
}

public class AnnouncementOperationsService : IAnnouncementOperationsService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AnnouncementOperationsService> _logger;
    private readonly IEmailService _emailService;
    private readonly IEmailCategoryService _emailCategoryService;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IAuditLogService _auditLog;
    private readonly string _uploadPath;

    public AnnouncementOperationsService(
        DeuEpostaContext context,
        ILogger<AnnouncementOperationsService> logger,
        IEmailService emailService,
        IEmailCategoryService emailCategoryService,
        ISystemSettingsService systemSettingsService,
        IAuditLogService auditLog,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _emailCategoryService = emailCategoryService;
        _systemSettingsService = systemSettingsService;
        _auditLog = auditLog;

        var uploadPath = configuration["FileSettings:UploadPath"] ?? "uploads";
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);
    }

    public async Task<ResponseModel> ChangeStatusAsync(int id, string yeniDurum, int kullaniciId, string? note = null)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // Eski durumu kaydet (log için)
            var eskiDurum = announcement.Durum;

            // Model'daki business logic method'u kullan - durum değişikliği ve validation dahil
            if (!announcement.TryChangeDurum(yeniDurum, out string? hata))
            {
                return ResponseModel.ErrorResult(hata ?? "Durum değiştirilemedi", 400);
            }

            if (yeniDurum == DuyuruDurum.ONAYLANDI)
            {
                announcement.SonOnaylayanKullaniciId = kullaniciId;
            }
            else if (yeniDurum == DuyuruDurum.GONDERILDI)
            {
                announcement.GercekGonderimTarihi = DateTime.Now;
            }

            // Hareket kaydı ekle
            _context.EpostaDuyuruHareketleri.Add(new EpostaDuyuruHareket
            {
                DuyuruId = id,
                OncekiDurum = eskiDurum,
                YeniDurum = yeniDurum,
                IslemTipi = "DURUM_DEGISIKLIGI",
                KullaniciId = kullaniciId,
                Aciklama = note,
                IslemTarihi = DateTime.Now
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement status changed: {Id} from {OldStatus} to {NewStatus} by user {UserId}",
                id, eskiDurum, yeniDurum, kullaniciId);

            return ResponseModel.SuccessResult($"Duyuru durumu '{yeniDurum}' olarak güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing announcement status {Id}", id);
            return ResponseModel.ErrorResult("Duyuru durumu güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<AnnouncementStatistics>> GetAnnouncementStatisticsAsync(int? kullaniciId = null)
    {
        try
        {
            var query = _context.EpostaDuyurulari
                .AsNoTracking() // Read-only statistics query
                .AsQueryable();

            // Kullanıcıya özgü filtrele
            if (kullaniciId.HasValue)
            {
                query = query.Where(d => d.OlusturanKullaniciId == kullaniciId.Value);
            }

            // Tek sorguda tüm istatistikleri al (N+1 problemi çözümü)
            var data = await query
                .GroupBy(d => 1) // Tüm kayıtları tek gruba al
                .Select(g => new
                {
                    TotalAnnouncements = g.Count(),
                    DraftCount = g.Count(d => d.Durum == DuyuruDurum.TASLAK),
                    PendingApprovalCount = g.Count(d => d.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR || d.Durum == DuyuruDurum.SON_ONAY_BEKLIYOR),
                    ApprovedCount = g.Count(d => d.Durum == DuyuruDurum.ONAYLANDI),
                    SentCount = g.Count(d => d.Durum == DuyuruDurum.GONDERILDI),
                    CancelledCount = g.Count(d => d.Durum == DuyuruDurum.IPTAL),
                    TotalRecipients = g.Sum(d => d.ToplamAliciSayisi),
                    SuccessfulSends = g.Sum(d => d.BasariliGonderimSayisi),
                    FailedSends = g.Sum(d => d.BasarisizGonderimSayisi)
                })
                .FirstOrDefaultAsync();

            var statistics = new AnnouncementStatistics
            {
                TotalAnnouncements = data?.TotalAnnouncements ?? 0,
                DraftCount = data?.DraftCount ?? 0,
                PendingApprovalCount = data?.PendingApprovalCount ?? 0,
                ApprovedCount = data?.ApprovedCount ?? 0,
                SentCount = data?.SentCount ?? 0,
                CancelledCount = data?.CancelledCount ?? 0,
                TotalRecipients = data?.TotalRecipients ?? 0,
                SuccessfulSends = data?.SuccessfulSends ?? 0,
                FailedSends = data?.FailedSends ?? 0
            };

            return ResponseDataModel<AnnouncementStatistics>.SuccessResult(statistics, "İstatistikler alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement statistics");
            return ResponseDataModel<AnnouncementStatistics>.ErrorResult("İstatistikler alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<EpostaDuyuruGonderimLog>>> GetSendLogsAsync(int announcementId)
    {
        try
        {
            var sendLogs = await _context.EpostaDuyuruGonderimLoglari
                .Where(l => l.DuyuruId == announcementId)
                .OrderBy(l => l.AliciKategorisi)
                .ThenBy(l => l.AliciEmail)
                .ToListAsync();

            return ResponseDataModel<List<EpostaDuyuruGonderimLog>>.SuccessResult(sendLogs, "Gönderim logları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting send logs for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<List<EpostaDuyuruGonderimLog>>.ErrorResult("Gönderim logları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ReactivateAnnouncementAsync(int id, int kullaniciId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.OlusturanKullanici)
                    .ThenInclude(k => k!.Rol)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // YETKI KONTROLÜ: Sadece ADMIN yeniden aktif edebilir
            var currentUser = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == kullaniciId);

            if (currentUser?.Rol?.RolKodu != RolKodu.ADMIN)
                return ResponseModel.ErrorResult("Sadece ADMIN iptal edilmiş duyuruları yeniden aktif edebilir", 403);

            // STATE MACHINE FIX: State machine validation kullan
            if (!announcement.CanTransitionTo(DuyuruDurum.ONAYLANDI))
            {
                var allowedTransitions = DuyuruDurum.GetAllowedTransitions(announcement.Durum);
                var allowedStates = string.Join(", ", allowedTransitions);
                return ResponseModel.ErrorResult(
                    $"'{announcement.Durum}' durumundan yeniden aktif edilemez. İzin verilen geçişler: {allowedStates}", 400);
            }

            // Onay bilgisi kontrolü - iptal edilen duyurunun daha önce onaylanmış olması gerekir
            if (!announcement.SonOnaylayanKullaniciId.HasValue)
                return ResponseModel.ErrorResult("İptal edilen duyuru daha önce onaylanmamış, yeniden aktif edilemez", 400);

            // Durumu ONAYLANDI'ya geri al
            announcement.Durum = DuyuruDurum.ONAYLANDI;
            announcement.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement reactivated: {Id} by user {UserId} from ", id, kullaniciId);

            // Audit log: Yeniden aktif etme
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_YENIDEN_AKTIF",
                detay: $"İptal edilen duyuru yeniden aktif edildi. ID: {id}, Konu: {announcement.Konu}"
            );

            return ResponseModel.SuccessResult("Duyuru yeniden aktif edildi. Artık zamanlanabilir veya gönderilebilir.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru yeniden aktif edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> SendTestEmailAsync(int id, int kullaniciId, string? testEmail)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Sablon)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // Yetki kontrolü
            var user = await _context.Kullanicilar
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == kullaniciId);
            if (user == null)
                return ResponseModel.ErrorResult("Kullanıcı bulunamadı", 404);

            var isAdmin = user.Rol?.RolKodu == RolKodu.ADMIN;
            var isOwnAnnouncement = announcement.OlusturanKullaniciId == kullaniciId;
            var isAssignedManager = announcement.SonOnaylayanKullaniciId == kullaniciId;
            var isCoordinatorForPending = user.Rol?.RolKodu == RolKodu.COORDINATOR &&
                                         announcement.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR;

            if (!isOwnAnnouncement && !isAdmin && !isAssignedManager && !isCoordinatorForPending)
                return ResponseModel.ErrorResult("Bu duyuruya test email gönderme yetkiniz yok", 403);

            // Test email adresi
            var targetEmail = !string.IsNullOrEmpty(testEmail) ? testEmail : user.Email;

            // Email subject ve body hazırla
            var subject = announcement.Konu;
            var body = announcement.Icerik;

            // Banner varsa HTML'in başına ekle
            if (announcement.BannerDosyaId.HasValue)
            {
                var bannerFile = await _context.Dosyalar.FirstOrDefaultAsync(f => f.Id == announcement.BannerDosyaId.Value);
                if (bannerFile != null)
                {
                    try
                    {
                        var filePath = Path.Combine(_uploadPath, bannerFile.DosyaYolu);
                        if (File.Exists(filePath))
                        {
                            var fileBytes = await File.ReadAllBytesAsync(filePath);
                            var base64Image = Convert.ToBase64String(fileBytes);
                            var mimeType = bannerFile.DosyaTipi ?? "image/png";
                            var dataUrl = $"data:{mimeType};base64,{base64Image}";

                            body = $"<div style='text-align:center;margin-bottom:20px;'><img src='{dataUrl}' alt='Banner' style='max-width:100%;height:auto;'/></div>{body}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to embed banner in test email for announcement {Id}", id);
                    }
                }
            }

            // İmza ekle
            var signature = await _systemSettingsService.GetEmailSignatureAsync(announcement.DuyuruKategorisi);
            if (!string.IsNullOrEmpty(signature))
            {
                body += signature;
            }

            // Test warning banner'ı ekle
            subject = "[TEST EMAILI] " + subject;
            body = $@"
<div style='background-color: #fff3cd; border: 2px solid #ffc107; padding: 15px; margin-bottom: 20px; border-radius: 5px;'>
    <h3 style='color: #856404; margin: 0 0 10px 0;'>⚠️ Bu bir test email'idir</h3>
    <p style='margin: 0; color: #856404;'>
        <strong>Konu:</strong> {announcement.Konu}<br>
        <strong>Gönderen:</strong> {user.AdSoyad}<br>
        <strong>Test Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}<br>
        <strong>Durum:</strong> {announcement.Durum}
    </p>
</div>

{body}

<hr style='margin-top: 30px; border: none; border-top: 1px solid #ddd;'>
<p style='color: #666; font-size: 12px; margin-top: 20px;'>
    <strong>Not:</strong> Bu test email'idir. Gerçek alıcılara gönderilmemiştir.
</p>";

            // Email gönder
            var emailRequest = new SendEmailRequest
            {
                ToRecipients = new List<string> { targetEmail },
                Subject = subject,
                Body = body,
                IsHtml = true,
                Category = announcement.GondericiKategori
            };

            var emailResult = await _emailService.SendEmailAsync(emailRequest);

            if (!emailResult)
            {
                return ResponseModel.ErrorResult("Test email gönderilemedi", 500);
            }

            _logger.LogInformation("Test email sent for announcement {AnnouncementId} to {Email} by user {UserId}",
                id, targetEmail, kullaniciId);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "TEST_EMAIL_GONDERILDI",
                detay: $"Test email gönderildi. Duyuru ID: {id}, Alıcı: {targetEmail}",
                kullaniciId: kullaniciId,
                ipAdres: "TEST"
            );

            return ResponseModel.SuccessResult($"Test email '{targetEmail}' adresine gönderildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email for announcement {Id}", id);
            return ResponseModel.ErrorResult("Test email gönderilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<AnnouncementPreviewDto>> GetAnnouncementPreviewAsync(int announcementId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Sablon)
                .Include(d => d.OlusturanKullanici)
                .FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseDataModel<AnnouncementPreviewDto>.ErrorResult("Duyuru bulunamadı", 404);

            // Get recipient stats
            var recipients = await _context.EpostaDuyuruAlicilari
                .Where(a => a.DuyuruId == announcementId)
                .ToListAsync();

            var recipientPreview = new RecipientPreviewDto
            {
                TotalRecipientCount = recipients.Count,
                ToCount = recipients.Count(r => r.AliciKategorisi == "TO"),
                CcCount = recipients.Count(r => r.AliciKategorisi == "CC"),
                BccCount = recipients.Count(r => r.AliciKategorisi == "BCC"),
                GroupCount = recipients.Count(r => r.GrupId.HasValue),
                ManualCount = recipients.Count(r => !r.GrupId.HasValue),
                Recipients = recipients.Take(100).Select(r => new RecipientPreviewItem
                {
                    Email = r.Email ?? "",
                    Name = r.Email,
                    Category = r.AliciKategorisi ?? "TO"
                }).ToList()
            };

            // Get attachments
            var attachments = await _context.Dosyalar
                .Where(f => f.DuyuruId == announcementId && f.Aktif == "Y")
                .Select(f => new AttachmentPreviewItem
                {
                    FileId = f.Id,
                    FileName = f.DosyaAdi,
                    FileSize = $"{f.DosyaBoyutu / 1024:N0} KB",
                    FileType = f.DosyaTipi
                })
                .ToListAsync();

            // Process template content
            var htmlContent = announcement.Sablon?.IcerikSablonu ?? announcement.Icerik;
            if (!string.IsNullOrEmpty(htmlContent))
            {
                // Replace template variables
                htmlContent = htmlContent
                    .Replace("{{konu}}", announcement.Konu)
                    .Replace("{{icerik}}", announcement.Icerik)
                    .Replace("{{tarih}}", DateTime.Now.ToString("dd/MM/yyyy"))
                    .Replace("{{gonderen}}", announcement.OlusturanKullanici?.AdSoyad ?? "DEÜ");
            }

            // Email kategori ayarlarını al (SMTP gönderici kategorisi kullanılmalı)
            var emailConfig = await _emailCategoryService.GetEmailConfigByCategoryAsync(announcement.GondericiKategori);

            // İmza ekle
            var signature = await _systemSettingsService.GetEmailSignatureAsync(announcement.DuyuruKategorisi);
            if (!string.IsNullOrEmpty(signature))
            {
                htmlContent += signature;
            }

            var preview = new AnnouncementPreviewDto
            {
                Subject = announcement.Konu,
                HtmlContent = htmlContent ?? announcement.Icerik,
                TextContent = System.Text.RegularExpressions.Regex.Replace(htmlContent ?? announcement.Icerik, "<.*?>", string.Empty),
                FromEmail = emailConfig.FromEmail,
                FromName = emailConfig.FromName,
                Recipients = recipientPreview,
                Attachments = attachments,
                TemplateName = announcement.Sablon?.SablonAdi ?? "Özel İçerik",
                CreatedDate = announcement.OlusturmaTarihi,
                CreatedBy = announcement.OlusturanKullanici?.AdSoyad ?? "Bilinmeyen"
            };

            return ResponseDataModel<AnnouncementPreviewDto>.SuccessResult(preview, "Duyuru önizlemesi alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement preview for {AnnouncementId}", announcementId);
            return ResponseDataModel<AnnouncementPreviewDto>.ErrorResult("Duyuru önizlemesi alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<AnnouncementMovementView>>> GetAnnouncementMovementsAsync(int announcementId)
    {
        try
        {
            var movements = await _context.EpostaDuyuruHareketleri
                .Include(h => h.Kullanici)
                .Include(h => h.SecilenOnaylayici)
                .Where(h => h.DuyuruId == announcementId)
                .OrderByDescending(h => h.IslemTarihi)
                .Select(h => new AnnouncementMovementView
                {
                    Id = h.Id,
                    DuyuruId = h.DuyuruId,
                    OncekiDurum = h.OncekiDurum,
                    YeniDurum = h.YeniDurum,
                    IslemTipi = h.IslemTipi,
                    KullaniciId = h.KullaniciId,
                    KullaniciAdi = h.Kullanici != null ? h.Kullanici.AdSoyad : null,
                    Aciklama = h.Aciklama,
                    SecilenOnaylayiciId = h.SecilenOnaylayiciId,
                    SecilenOnaylayiciAdi = h.SecilenOnaylayici != null ? h.SecilenOnaylayici.AdSoyad : null,
                    IslemTarihi = h.IslemTarihi
                })
                .ToListAsync();

            return ResponseDataModel<List<AnnouncementMovementView>>.SuccessResult(movements, "Duyuru hareketleri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement movements for {AnnouncementId}", announcementId);
            return ResponseDataModel<List<AnnouncementMovementView>>.ErrorResult("Duyuru hareketleri alınırken hata oluştu", 500);
        }
    }
}