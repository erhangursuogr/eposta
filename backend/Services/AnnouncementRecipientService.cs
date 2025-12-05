using AutoMapper;
using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface IAnnouncementRecipientService
{
    Task<ResponseDataModel<List<EpostaDuyuruAlici>>> GetRecipientsAsync(int announcementId);

    Task<ResponseDataModel<bool>> CanModifyRecipientsAsync(int announcementId);

    Task<ResponseDataModel<RecipientStatsDto>> GetRecipientStatsAsync(int announcementId);

    Task<ResponseModel> ReplaceGroupRecipientsAsync(int announcementId, int groupId);

    Task<ResponseModel> AddManualRecipientAsync(int announcementId, AddManualRecipientRequest request);

    Task<ResponseModel> RemoveRecipientAsync(int announcementId, int recipientId);

    Task<ResponseDataModel<RecipientPreviewDto>> GetRecipientPreviewAsync(int announcementId);

    Task<ResponseDataModel<int>> GetSentRecipientCountAsync(int announcementId);
}

public class AnnouncementRecipientService : IAnnouncementRecipientService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AnnouncementRecipientService> _logger;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLog;

    public AnnouncementRecipientService(
        DeuEpostaContext context,
        ILogger<AnnouncementRecipientService> logger,
        IMapper mapper,
        IEmailService emailService,
        IAuditLogService auditLog)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _emailService = emailService;
        _auditLog = auditLog;
    }

    public async Task<ResponseDataModel<List<EpostaDuyuruAlici>>> GetRecipientsAsync(int announcementId)
    {
        try
        {
            var recipients = await _context.EpostaDuyuruAlicilari
                .Include(a => a.Grup)
                .Where(a => a.DuyuruId == announcementId)
                .ToListAsync();

            return ResponseDataModel<List<EpostaDuyuruAlici>>.SuccessResult(recipients, "Alıcı listesi alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipients for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<List<EpostaDuyuruAlici>>.ErrorResult("Alıcı listesi alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<bool>> CanModifyRecipientsAsync(int announcementId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseDataModel<bool>.ErrorResult("Duyuru bulunamadı", 404);

            bool canModify = announcement.Durum == DuyuruDurum.TASLAK;

            return ResponseDataModel<bool>.SuccessResult(canModify, "Alıcı değiştirme yetkisi kontrol edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recipient modification permission for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<bool>.ErrorResult("Alıcı değiştirme yetkisi kontrol edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<RecipientStatsDto>> GetRecipientStatsAsync(int announcementId)
    {
        try
        {
            var recipients = await _context.EpostaDuyuruAlicilari
                .Where(a => a.DuyuruId == announcementId)
                .ToListAsync();

            var stats = new RecipientStatsDto
            {
                TotalCount = recipients.Count,
                ToCount = recipients.Count(r => r.AliciKategorisi == "TO"),
                CcCount = recipients.Count(r => r.AliciKategorisi == "CC"),
                BccCount = recipients.Count(r => r.AliciKategorisi == "BCC"),
                GroupCount = recipients.Count(r => r.GrupId.HasValue),
                ManualCount = recipients.Count(r => !r.GrupId.HasValue)
            };

            return ResponseDataModel<RecipientStatsDto>.SuccessResult(stats, "Alıcı istatistikleri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipient stats for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<RecipientStatsDto>.ErrorResult("Alıcı istatistikleri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ReplaceGroupRecipientsAsync(int announcementId, int groupId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.TASLAK)
                return ResponseModel.ErrorResult("Bu durumdaki duyurunun alıcı listesi değiştirilemez", 400);

            // Get the group to check type restrictions
            var group = await _context.EpostaGruplari.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return ResponseModel.ErrorResult("Grup bulunamadı", 404);

            // Determine recipient category based on group type
            var category = DetermineRecipientCategory(group.GrupTipi);

            // Remove existing group recipients
            var existingGroupRecipients = await _context.EpostaDuyuruAlicilari
                .Where(a => a.DuyuruId == announcementId && a.GrupId.HasValue)
                .ToListAsync();

            _context.EpostaDuyuruAlicilari.RemoveRange(existingGroupRecipients);

            // Add new group recipient with proper category
            // IMPORTANT: EMAIL must be NULL for GRUP type per database constraint
            var newRecipient = new EpostaDuyuruAlici
            {
                DuyuruId = announcementId,
                GrupId = groupId,
                AliciKategorisi = category,
                AliciTipi = "GRUP",
                Email = null, // GRUP tipi için EMAIL NULL olmalı (constraint requirement)
                AdSoyad = null, // GRUP tipi için AD_SOYAD da NULL
                OlusturmaTarihi = DateTime.Now
            };

            _context.EpostaDuyuruAlicilari.Add(newRecipient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Group recipients replaced for announcement {AnnouncementId} with group {GroupId}", announcementId, groupId);

            return ResponseModel.SuccessResult("Grup alıcıları değiştirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing group recipients for announcement {AnnouncementId}", announcementId);
            return ResponseModel.ErrorResult("Grup alıcıları değiştirilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> AddManualRecipientAsync(int announcementId, AddManualRecipientRequest request)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.TASLAK)
                return ResponseModel.ErrorResult("Bu durumdaki duyurunun alıcı listesi değiştirilemez", 400);

            // Check if recipient already exists
            var existingRecipient = await _context.EpostaDuyuruAlicilari
                .FirstOrDefaultAsync(a => a.DuyuruId == announcementId && a.Email == request.Email);

            if (existingRecipient != null)
                return ResponseModel.ErrorResult("Bu alıcı zaten listede mevcut", 409);

            // Validate recipient category (manual recipients are flexible, no group restrictions)
            if (!new[] { "TO", "CC", "BCC" }.Contains(request.Kategori.ToUpperInvariant()))
                return ResponseModel.ErrorResult("Geçersiz alıcı kategorisi. TO, CC veya BCC olmalıdır.", 400);

            var newRecipient = new EpostaDuyuruAlici
            {
                DuyuruId = announcementId,
                Email = request.Email,
                AdSoyad = request.AdSoyad,
                AliciKategorisi = request.Kategori,
                OlusturmaTarihi = DateTime.Now,
                AliciTipi = "MANUEL"
            };

            _context.EpostaDuyuruAlicilari.Add(newRecipient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Manual recipient added to announcement {AnnouncementId}: {Email}", announcementId, request.Email);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "ALICI_EKLEME",
                detay: $"Duyuru ID: {announcementId}, Alıcı: {request.Email} ({request.Kategori}), Ad Soyad: {request.AdSoyad ?? "Yok"}",
                kullaniciId: announcement.OlusturanKullaniciId
            );

            return ResponseModel.SuccessResult("Manuel alıcı eklendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding manual recipient to announcement {AnnouncementId}", announcementId);
            return ResponseModel.ErrorResult("Manuel alıcı eklenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> RemoveRecipientAsync(int announcementId, int recipientId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            if (announcement.Durum != DuyuruDurum.TASLAK)
                return ResponseModel.ErrorResult("Bu durumdaki duyurunun alıcı listesi değiştirilemez", 400);

            var recipient = await _context.EpostaDuyuruAlicilari
                .FirstOrDefaultAsync(a => a.Id == recipientId && a.DuyuruId == announcementId);

            if (recipient == null)
                return ResponseModel.ErrorResult("Alıcı bulunamadı", 404);

            var recipientEmail = recipient.Email;
            var recipientCategory = recipient.AliciKategorisi;

            _context.EpostaDuyuruAlicilari.Remove(recipient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Recipient removed from announcement {AnnouncementId}: {RecipientId}", announcementId, recipientId);

            // Audit log
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "ALICI_SILME",
                detay: $"Duyuru ID: {announcementId}, Alıcı: {recipientEmail} ({recipientCategory}), Alıcı ID: {recipientId}",
                kullaniciId: announcement.OlusturanKullaniciId
            );

            return ResponseModel.SuccessResult("Alıcı kaldırıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing recipient {RecipientId} from announcement {AnnouncementId}", recipientId, announcementId);
            return ResponseModel.ErrorResult("Alıcı kaldırılırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<RecipientPreviewDto>> GetRecipientPreviewAsync(int announcementId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari.FirstOrDefaultAsync(d => d.Id == announcementId);
            if (announcement == null)
                return ResponseDataModel<RecipientPreviewDto>.ErrorResult("Duyuru bulunamadı", 404);

            var recipients = await _context.EpostaDuyuruAlicilari
                .Include(a => a.Grup)
                .Where(a => a.DuyuruId == announcementId)
                .ToListAsync();

            var preview = new RecipientPreviewDto();
            var recipientItems = new List<RecipientPreviewItem>();
            var groupItems = new List<GroupPreviewItem>();

            var allEmails = new HashSet<string>();

            // PERFORMANS OPTİMİZASYONU: Paralel grup email çözümlemesi
            var groupRecipients = recipients.Where(r => r.GrupId.HasValue && r.Grup != null).ToList();

            // Tüm grup email'lerini paralel olarak al
            var groupEmailTasks = groupRecipients.Select(async recipient =>
            {
                var emails = await _emailService.GetSmartRecipientsAsync(recipient.GrupId!.Value, recipient.AliciKategorisi);
                return (recipient, emails);
            });
            var groupEmailResults = await Task.WhenAll(groupEmailTasks);

            // Sonuçları işle
            foreach (var (recipient, groupEmails) in groupEmailResults)
            {
                var groupItem = new GroupPreviewItem
                {
                    GroupId = recipient.Grup!.Id,
                    GroupName = recipient.Grup.GrupAdi,
                    GroupType = recipient.Grup.GrupTipi,
                    Category = recipient.AliciKategorisi,
                    MemberCount = groupEmails.Count,
                    IsBccOnly = DeuEposta.Models.Enums.GrupTipiExtensions.ParseSafely(recipient.Grup.GrupTipi).IsBccOnly()
                };
                groupItems.Add(groupItem);

                // Add individual emails from group
                foreach (var email in groupEmails)
                {
                    if (allEmails.Add(email)) // Add only if not duplicate
                    {
                        recipientItems.Add(new RecipientPreviewItem
                        {
                            Email = email,
                            Name = null, // Email service sadece email döner, isim yok
                            Category = recipient.AliciKategorisi,
                            Source = "GRUP",
                            GroupName = recipient.Grup.GrupAdi
                        });
                    }
                }
            }

            // Manuel alıcıları işle
            foreach (var recipient in recipients.Where(r => !r.GrupId.HasValue && !string.IsNullOrEmpty(r.Email)))
            {
                // Manual recipient
                if (allEmails.Add(recipient.Email)) // Add only if not duplicate
                {
                    recipientItems.Add(new RecipientPreviewItem
                    {
                        Email = recipient.Email,
                        Name = recipient.AdSoyad,
                        Category = recipient.AliciKategorisi,
                        Source = "MANUAL"
                    });
                }
            }

            // Calculate statistics
            preview.TotalRecipientCount = allEmails.Count;
            preview.ToCount = recipientItems.Count(r => r.Category == "TO");
            preview.CcCount = recipientItems.Count(r => r.Category == "CC");
            preview.BccCount = recipientItems.Count(r => r.Category == "BCC");
            preview.GroupCount = recipientItems.Count(r => r.Source == "GROUP");
            preview.ManualCount = recipientItems.Count(r => r.Source == "MANUAL");

            preview.Recipients = recipientItems.OrderBy(r => r.Email).ToList();
            preview.Groups = groupItems.OrderBy(g => g.GroupName).ToList();

            return ResponseDataModel<RecipientPreviewDto>.SuccessResult(preview, "Alıcı önizlemesi alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipient preview for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<RecipientPreviewDto>.ErrorResult("Alıcı önizlemesi alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<int>> GetSentRecipientCountAsync(int announcementId)
    {
        try
        {
            var sentCount = await _context.EpostaDuyuruGonderimLoglari
                .Where(l => l.DuyuruId == announcementId && l.GonderimDurumu == "BASARILI")
                .CountAsync();

            return ResponseDataModel<int>.SuccessResult(sentCount, "Gönderilen alıcı sayısı alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sent recipient count for announcement {AnnouncementId}", announcementId);
            return ResponseDataModel<int>.ErrorResult("Gönderilen alıcı sayısı alınırken hata oluştu", 500);
        }
    }

    private static string DetermineRecipientCategory(string grupTipi)
    {
        var parsedTip = GrupTipiExtensions.ParseSafely(grupTipi);
        return parsedTip.IsBccOnly() ? "BCC" : "TO";
    }
}