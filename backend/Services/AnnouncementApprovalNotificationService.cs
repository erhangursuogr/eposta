using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services
{
    public interface IAnnouncementApprovalNotificationService
    {
        Task SendSubmittedForApprovalNotificationAsync(EpostaDuyuru announcement, int submittedByUserId);

        Task SendApprovedNotificationAsync(EpostaDuyuru announcement, int approvedByUserId, string? approvalNote);

        Task SendRejectedNotificationAsync(EpostaDuyuru announcement, int rejectedByUserId, string rejectionReason);

        Task SendCancelledNotificationAsync(EpostaDuyuru announcement, int cancelledByUserId, string cancellationReason);

        Task SendApprovalNotificationEmailAsync(EpostaDuyuru announcement, string onaylayanKisi, string rolAdi, string? onayNotu = null);

        Task SendRejectionNotificationEmailAsync(EpostaDuyuru announcement, string reddenKisi, string redNedeni, string rolAdi);
    }

    public class AnnouncementApprovalNotificationService : IAnnouncementApprovalNotificationService
    {
        private readonly DeuEpostaContext _context;
        private readonly ILogger<AnnouncementApprovalService> _logger;
        private readonly IEmailService _emailService;

        public AnnouncementApprovalNotificationService(
        DeuEpostaContext context,
        ILogger<AnnouncementApprovalService> logger,
        IEmailService emailService,
        IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task SendSubmittedForApprovalNotificationAsync(EpostaDuyuru announcement, int submittedByUserId)
        {
            try
            {
                var submitter = await _context.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Id == submittedByUserId);

                if (submitter == null)
                {
                    _logger.LogWarning("Submitter not found for announcement {AnnouncementId}", announcement.Id);
                    return;
                }

                List<string> recipientEmails = new List<string>();

                // ILK_ONAY_BEKLIYOR: Tüm Kontrolörlere mail gönder
                if (announcement.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR && !announcement.SonOnaylayanKullaniciId.HasValue)
                {
                    var coordinators = await _context.Kullanicilar
                        .Include(k => k.Rol)
                        .Where(k => k.Aktif == "Y" && k.Rol != null && k.Rol.RolKodu == RolKodu.COORDINATOR)
                        .ToListAsync();

                    if (!coordinators.Any())
                    {
                        _logger.LogWarning("No coordinators found for first approval notification");
                        return;
                    }

                    // Kendisi hariç tüm Kontrolörlerin emaillerini ekle
                    recipientEmails = coordinators
                        .Where(c => c.Id != submittedByUserId)
                        .Select(c => c.Email)
                        .ToList();

                    if (!recipientEmails.Any())
                    {
                        _logger.LogInformation("No coordinators to notify (submitter is coordinator)");
                        return;
                    }
                }
                // SON_ONAY_BEKLIYOR: Atanan manager'a mail gönder
                else if (announcement.SonOnaylayanKullaniciId.HasValue)
                {
                    // Kendisine göndermeye gerek yok
                    if (submittedByUserId == announcement.SonOnaylayanKullaniciId.Value)
                    {
                        _logger.LogInformation("Submitter is the approver, skipping notification for announcement {Id}", announcement.Id);
                        return;
                    }

                    var approver = await _context.Kullanicilar
                        .FirstOrDefaultAsync(u => u.Id == announcement.SonOnaylayanKullaniciId.Value);

                    if (approver == null)
                    {
                        _logger.LogWarning("Approver not found for announcement {AnnouncementId}", announcement.Id);
                        return;
                    }

                    recipientEmails.Add(approver.Email);
                }
                else
                {
                    _logger.LogWarning("No approver set and status is not ILK_ONAY_BEKLIYOR for announcement {AnnouncementId}", announcement.Id);
                    return;
                }

                var approvalStageText = announcement.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR
                    ? "İlk Onay (Kontrolör)"
                    : "Son Onay (Yönetici)";

                var subject = $"[DEÜ Duyuru Sistemi] {approvalStageText} - Onay Bekleyen Duyuru: {announcement.Konu}";
                var body = $@"
    <h2>Onayınızı Bekleyen Duyuru : {approvalStageText}</h2>
    <p><strong>{submitter.AdSoyad}</strong> tarafından oluşturulan bir duyuru onayınızı bekliyor.</p>

    <div style='border: 1px solid #ffc107; padding: 15px; margin: 10px 0; background-color: #fff3cd;'>
        <h3>{announcement.Konu}</h3>
        <p><strong>Konu:</strong> {announcement.Konu}</p>
        <p><strong>Gönderici:</strong> {submitter.AdSoyad}</p>
        <p><strong>Gönderim Tarihi:</strong> {announcement.OlusturmaTarihi:dd/MM/yyyy HH:mm}</p>
        <p><strong>Onay Aşaması:</strong> {approvalStageText}</p>
    </div>
    <p>
      Duyuruyu incelemek ve onaylamak için lütfen sisteme
      <a href=""https://kurumsalduyuru.deu.edu.tr"" target=""_blank"" rel=""noopener noreferrer"">
        giriş
      </a>
      yapınız.
    </p>
    <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
    <p style='font-size: 12px; color: #6c757d;'>
        Bu e-posta DEÜ Duyuru Yönetim Sistemi tarafından otomatik olarak gönderilmiştir.<br>
        Lütfen bu e-postayı yanıtlamayınız.
    </p>";

                var emailRequest = new SendEmailRequest
                {
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    ToRecipients = recipientEmails,
                    Category = "EMAIL_SISTEM" // Sistem bildirimleri için özel kategori
                };

                await _emailService.SendEmailAsync(emailRequest);
                _logger.LogInformation("Submitted for approval notification sent to {RecipientCount} recipient(s) for announcement {AnnouncementId}",
                    recipientEmails.Count, announcement.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending submitted for approval notification for announcement {AnnouncementId}", announcement.Id);
            }
        }

        public async Task SendApprovedNotificationAsync(EpostaDuyuru announcement, int approvedByUserId, string? approvalNote)
        {
            try
            {
                // Onaylayan kullanıcı bilgisini getir
                var approver = await _context.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Id == approvedByUserId);

                if (approver == null)
                {
                    _logger.LogWarning("Approver not found: {ApprovedByUserId}", approvedByUserId);
                    return;
                }

                // Oluşturan kullanıcı bilgisini getir
                var creator = await _context.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Id == announcement.OlusturanKullaniciId);

                if (creator == null)
                {
                    _logger.LogWarning("Creator not found for announcement {AnnouncementId}", announcement.Id);
                    return;
                }

                // Kendisine göndermeye gerek yok
                if (approvedByUserId == announcement.OlusturanKullaniciId)
                {
                    _logger.LogInformation("Approver is the creator, skipping approved notification for announcement {Id}", announcement.Id);
                    return;
                }

                var subject = $"[DEÜ Duyuru Sistemi] Duyuru Onaylandı: {announcement.Konu}";

                var noteSection = !string.IsNullOrEmpty(approvalNote)
                    ? $"<p><strong>Onay Notu:</strong> {approvalNote}</p>"
                    : "";

                var body = $@"
<h2>Duyuru Onaylandı</h2>
<p>Merhaba {creator.AdSoyad},</p>
<p>Duyurunuz <strong>{approver.AdSoyad}</strong> tarafından onaylandı.</p>

<div style='border: 1px solid #28a745; padding: 15px; margin: 10px 0; background-color: #d4edda;'>
<h3>{announcement.Konu}</h3>
<p><strong>Konu:</strong> {announcement.Konu}</p>
<p><strong>Onay Tarihi:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
<p><strong>Onaylayan:</strong> {approver.AdSoyad}</p>
{noteSection}
</div>

<p>Artık duyurunuzu hemen gönderebilir veya zamanlanmış gönderim yapabilirsiniz.</p>
<p>
Duyuruyu incelemek için lütfen sisteme
<a href=""https://kurumsalduyuru.deu.edu.tr"" target=""_blank"" rel=""noopener noreferrer"">
giriş
</a>
yapınız.
</p>
<hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
<p style='font-size: 12px; color: #6c757d;'>
Bu e-posta DEÜ Duyuru Yönetim Sistemi tarafından otomatik olarak gönderilmiştir.<br>
Lütfen bu e-postayı yanıtlamayınız.
</p>";

                var emailRequest = new SendEmailRequest
                {
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    ToRecipients = new List<string> { creator.Email },
                    Category = "EMAIL_SISTEM" // Sistem bildirimleri için özel kategori
                };

                await _emailService.SendEmailAsync(emailRequest);
                _logger.LogInformation("Approval notification sent to creator {CreatorEmail} for announcement {AnnouncementId}",
                    creator.Email, announcement.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval notification for announcement {AnnouncementId}", announcement.Id);
            }
        }

        public async Task SendRejectedNotificationAsync(EpostaDuyuru announcement, int rejectedByUserId, string rejectionReason)
        {
            try
            {
                // Reddeden kullanıcı bilgisini getir
                var rejecter = await _context.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Id == rejectedByUserId);

                if (rejecter == null)
                {
                    _logger.LogWarning("Rejecter not found: {RejectedByUserId}", rejectedByUserId);
                    return;
                }

                // Oluşturan kullanıcı bilgisini getir
                var creator = await _context.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Id == announcement.OlusturanKullaniciId);

                if (creator == null)
                {
                    _logger.LogWarning("Creator not found for announcement {AnnouncementId}", announcement.Id);
                    return;
                }

                // Kendisine göndermeye gerek yok
                if (rejectedByUserId == announcement.OlusturanKullaniciId)
                {
                    _logger.LogInformation("Rejecter is the creator, skipping rejection notification for announcement {Id}", announcement.Id);
                    return;
                }

                var subject = $"[DEÜ Duyuru Sistemi] Duyuru Reddedildi: {announcement.Konu}";

                var body = $@"
<h2>Duyuru Reddedildi</h2>
<p>Merhaba {creator.AdSoyad},</p>
<p>Duyurunuz <strong>{rejecter.AdSoyad}</strong> tarafından reddedildi.</p>

<div style='border: 1px solid #dc3545; padding: 15px; margin: 10px 0; background-color: #f8d7da;'>
<h3>{announcement.Konu}</h3>
<p><strong>Konu:</strong> {announcement.Konu}</p>
<p><strong>Reddetme Tarihi:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
<p><strong>Reddeden:</strong> {rejecter.AdSoyad}</p>
<p><strong>Red Nedeni:</strong> {rejectionReason}</p>
</div>

<p>Duyurunuz <strong>REDDEDİLDİ</strong> durumuna alınmıştır. Gerekli düzeltmeleri yapıp tekrar onaya gönderebilirsiniz.</p>
<p>
Duyuruyu incelemek için lütfen sisteme
<a href=""https://kurumsalduyuru.deu.edu.tr"" target=""_blank"" rel=""noopener noreferrer"">
giriş
</a>
yapınız.
</p>
<hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
<p style='font-size: 12px; color: #6c757d;'>
Bu e-posta DEÜ Duyuru Yönetim Sistemi tarafından otomatik olarak gönderilmiştir.<br>
Lütfen bu e-postayı yanıtlamayınız.
</p>";

                var emailRequest = new SendEmailRequest
                {
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    ToRecipients = new List<string> { creator.Email },
                    Category = "EMAIL_SISTEM" // Sistem bildirimleri için özel kategori
                };

                await _emailService.SendEmailAsync(emailRequest);
                _logger.LogInformation("Rejection notification sent to creator {CreatorEmail} for announcement {AnnouncementId}",
                    creator.Email, announcement.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending rejection notification for announcement {AnnouncementId}", announcement.Id);
            }
        }

        public async Task SendCancelledNotificationAsync(EpostaDuyuru announcement, int cancelledByUserId, string cancellationReason)
        {
            try
            {
                // İptal eden kullanıcı bilgisini getir
                var canceller = await _context.Kullanicilar
                    .FirstOrDefaultAsync(u => u.Id == cancelledByUserId);

                if (canceller == null)
                {
                    _logger.LogWarning("Canceller not found: {CancelledByUserId}", cancelledByUserId);
                    return;
                }

                var recipientEmails = new List<string>();

                // 1. Oluşturan kullanıcıya bildirim gönder (kendisi iptal etmediyse)
                if (announcement.OlusturanKullaniciId != cancelledByUserId)
                {
                    var creator = await _context.Kullanicilar
                        .FirstOrDefaultAsync(u => u.Id == announcement.OlusturanKullaniciId);

                    if (creator != null)
                    {
                        recipientEmails.Add(creator.Email);
                    }
                }

                // 2. Onaylayan kullanıcılara bildirim gönder (koordinatör ve manager - varsa ve farklı kullanıcılarysa)
                if (announcement.IlkOnaylayanKullaniciId.HasValue &&
                    announcement.IlkOnaylayanKullaniciId.Value != cancelledByUserId &&
                    announcement.IlkOnaylayanKullaniciId.Value != announcement.OlusturanKullaniciId)
                {
                    var coordinator = await _context.Kullanicilar
                        .FirstOrDefaultAsync(u => u.Id == announcement.IlkOnaylayanKullaniciId.Value);

                    if (coordinator != null && !recipientEmails.Contains(coordinator.Email))
                    {
                        recipientEmails.Add(coordinator.Email);
                    }
                }

                if (announcement.SonOnaylayanKullaniciId.HasValue &&
                    announcement.SonOnaylayanKullaniciId.Value != cancelledByUserId &&
                    announcement.SonOnaylayanKullaniciId.Value != announcement.OlusturanKullaniciId)
                {
                    var manager = await _context.Kullanicilar
                        .FirstOrDefaultAsync(u => u.Id == announcement.SonOnaylayanKullaniciId.Value);

                    if (manager != null && !recipientEmails.Contains(manager.Email))
                    {
                        recipientEmails.Add(manager.Email);
                    }
                }

                if (!recipientEmails.Any())
                {
                    _logger.LogInformation("No recipients for cancellation notification (self-cancellation) for announcement {Id}", announcement.Id);
                    return;
                }

                var subject = $"[DEÜ Duyuru Sistemi] Duyuru İptal Edildi: {announcement.Konu}";

                var body = $@"
<h2>Duyuru İptal Edildi</h2>
<p>Bir duyuru <strong>{canceller.AdSoyad}</strong> tarafından iptal edildi.</p>

<div style='border: 1px solid #dc3545; padding: 15px; margin: 10px 0; background-color: #f8d7da;'>
<h3>{announcement.Konu}</h3>
<p><strong>Konu:</strong> {announcement.Konu}</p>
<p><strong>İptal Tarihi:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
<p><strong>İptal Eden:</strong> {canceller.AdSoyad}</p>
<p><strong>İptal Nedeni:</strong> {cancellationReason}</p>
</div>
<p>
Duyuruyu incelemek için lütfen sisteme
<a href=""https://kurumsalduyuru.deu.edu.tr"" target=""_blank"" rel=""noopener noreferrer"">
giriş
</a>
yapınız.
</p>
<hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
<p style='font-size: 12px; color: #6c757d;'>
Bu e-posta DEÜ Duyuru Yönetim Sistemi tarafından otomatik olarak gönderilmiştir.<br>
Lütfen bu e-postayı yanıtlamayınız.
</p>";

                var emailRequest = new SendEmailRequest
                {
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    ToRecipients = recipientEmails,
                    Category = "EMAIL_SISTEM" // Sistem bildirimleri için özel kategori
                };

                await _emailService.SendEmailAsync(emailRequest);
                _logger.LogInformation("Cancellation notification sent to {RecipientCount} recipients for announcement {AnnouncementId}",
                    recipientEmails.Count, announcement.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cancellation notification for announcement {AnnouncementId}", announcement.Id);
            }
        }

        /// <summary>
        /// Duyuru onaylandığında oluşturucuya bilgilendirme maili gönderir
        /// </summary>
        public async Task SendApprovalNotificationEmailAsync(EpostaDuyuru announcement, string onaylayanKisi, string rolAdi, string? onayNotu = null)
        {
            try
            {
                if (announcement.OlusturanKullanici == null || string.IsNullOrEmpty(announcement.OlusturanKullanici.Email))
                {
                    _logger.LogWarning("Duyuru {Id} için oluşturucu bilgisi bulunamadı, onay maili gönderilemedi", announcement.Id);
                    return;
                }

                var subject = $"Duyuru Onaylandı: {announcement.Konu}";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .info-box {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50; }}
        .note-box {{ background-color: #e3f2fd; padding: 15px; margin: 15px 0; border-left: 4px solid #2196F3; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        strong {{ color: #388E3C; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>✅ Duyurunuz Onaylandı</h2>
        </div>
        <div class='content'>
            <p>Merhaba {announcement.OlusturanKullanici.AdSoyad},</p>
            <p>Oluşturduğunuz duyuru <strong>{rolAdi}</strong> tarafından onaylanmıştır.</p>

            <div class='info-box'>
                <p><strong>Duyuru Adı:</strong> {announcement.Konu}</p>
                <p><strong>Konu:</strong> {announcement.Konu}</p>
                <p><strong>Oluşturma Tarihi:</strong> {announcement.OlusturmaTarihi:dd.MM.yyyy HH:mm}</p>
                <p><strong>Onaylayan:</strong> {onaylayanKisi} ({rolAdi})</p>
                <p><strong>Onay Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>

            {(string.IsNullOrEmpty(onayNotu) ? "" : $@"
            <div class='note-box'>
                <p><strong>Onay Notu:</strong></p>
                <p>{onayNotu}</p>
            </div>
            ")}

            {(announcement.Durum == DuyuruDurum.ONAYLANDI ?
                    "<p>Duyurunuz <strong>ONAYLANDI</strong> durumundadır. Artık gönderilebilir.</p>" :
                    "<p>Duyurunuz <strong>SON ONAY BEKLİYOR</strong> durumundadır. Yönetici onayından sonra gönderim yapılabilir.</p>"
                )}

            <p>Sisteme giriş yapmak için: <a href='https://kurumsalduyuru.deu.edu.tr'>DEÜ Eposta Yönetim Sistemi</a></p>
        </div>
        <div class='footer'>
            <p>Bu otomatik bir bilgilendirme mesajıdır. Lütfen yanıtlamayınız.</p>
            <p>DEÜ Duyuru Yönetim Sistemi</p>
        </div>
    </div>
</body>
</html>";

                var emailRequest = new SendEmailRequest
                {
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    ToRecipients = new List<string> { announcement.OlusturanKullanici.Email },
                    Category = "EMAIL_SISTEM" // Sistem bildirimleri için özel kategori
                };

                var sent = await _emailService.SendEmailAsync(emailRequest);

                if (sent)
                {
                    _logger.LogInformation("Onay bilgilendirme maili gönderildi: Duyuru={Id}, Alıcı={Email}",
                        announcement.Id, announcement.OlusturanKullanici.Email);
                }
                else
                {
                    _logger.LogWarning("Onay bilgilendirme maili gönderilemedi: Duyuru={Id}", announcement.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onay bilgilendirme maili gönderilirken hata oluştu: Duyuru={Id}", announcement.Id);
            }
        }

        /// <summary>
        /// Duyuru reddedildiğinde oluşturucuya bilgilendirme maili gönderir
        /// </summary>
        public async Task SendRejectionNotificationEmailAsync(EpostaDuyuru announcement, string reddenKisi, string redNedeni, string rolAdi)
        {
            try
            {
                if (announcement.OlusturanKullanici == null || string.IsNullOrEmpty(announcement.OlusturanKullanici.Email))
                {
                    _logger.LogWarning("Duyuru {Id} için oluşturucu bilgisi bulunamadı, red maili gönderilemedi", announcement.Id);
                    return;
                }

                var subject = $"Duyuru Reddedildi: {announcement.Konu}";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }}
        .info-box {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #f44336; }}
        .reason-box {{ background-color: #fff3cd; padding: 15px; margin: 15px 0; border-left: 4px solid #ffc107; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        strong {{ color: #d32f2f; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🚫 Duyurunuz Reddedildi</h2>
        </div>
        <div class='content'>
            <p>Merhaba {announcement.OlusturanKullanici.AdSoyad},</p>
            <p>Oluşturduğunuz duyuru <strong>{rolAdi}</strong> tarafından reddedilmiştir.</p>

            <div class='info-box'>
                <p><strong>Duyuru Adı:</strong> {announcement.Konu}</p>
                <p><strong>Konu:</strong> {announcement.Konu}</p>
                <p><strong>Oluşturma Tarihi:</strong> {announcement.OlusturmaTarihi:dd.MM.yyyy HH:mm}</p>
                <p><strong>Reddeden:</strong> {reddenKisi} ({rolAdi})</p>
                <p><strong>Red Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>

            <div class='reason-box'>
                <p><strong>Red Nedeni:</strong></p>
                <p>{redNedeni}</p>
            </div>

            <p>Duyurunuz <strong>TASLAK</strong> durumuna döndürülmüştür. Red nedenini inceleyerek gerekli düzeltmeleri yapabilir ve tekrar onaya gönderebilirsiniz.</p>

            <p>Sisteme giriş yapmak için: <a href='https://kurumsalduyuru.deu.edu.tr'>DEÜ Eposta Yönetim Sistemi</a></p>
        </div>
        <div class='footer'>
            <p>Bu otomatik bir bilgilendirme mesajıdır. Lütfen yanıtlamayınız.</p>
            <p>DEÜ Duyuru Yönetim Sistemi</p>
        </div>
    </div>
</body>
</html>";

                var emailRequest = new SendEmailRequest
                {
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    ToRecipients = new List<string> { announcement.OlusturanKullanici.Email },
                    Category = "EMAIL_SISTEM" // Sistem bildirimleri için özel kategori // Sistem bildirimleri için kategori
                };

                var sent = await _emailService.SendEmailAsync(emailRequest);

                if (sent)
                {
                    _logger.LogInformation("Red bilgilendirme maili gönderildi: Duyuru={Id}, Alıcı={Email}",
                        announcement.Id, announcement.OlusturanKullanici.Email);
                }
                else
                {
                    _logger.LogWarning("Red bilgilendirme maili gönderilemedi: Duyuru={Id}", announcement.Id);
                }
            }
            catch (Exception ex)
            {
                // Email hatası ana işlemi etkilemez, sadece log'la
                _logger.LogError(ex, "Red bilgilendirme maili gönderilirken hata oluştu: Duyuru={Id}", announcement.Id);
            }
        }
    }
}