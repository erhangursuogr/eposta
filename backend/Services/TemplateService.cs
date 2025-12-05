using AutoMapper;
using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using DeuEposta.Attributes;

namespace DeuEposta.Services;

public interface ITemplateService
{
    Task<ResponseDataModel<List<EpostaSablon>>> GetTemplatesAsync();
    Task<ResponseDataModel<List<EpostaSablon>>> GetActiveTemplatesAsync();
    Task<ResponseDataModel<TemplateDetailView>> GetTemplateByIdAsync(int id);
    Task<ResponseModel> CreateTemplateAsync(CreateTemplateRequest request, int kullaniciId);
    Task<ResponseModel> UpdateTemplateAsync(int id, UpdateTemplateRequest request, int kullaniciId);
    Task<ResponseModel> DeleteTemplateAsync(int id, int kullaniciId);
    Task<ResponseModel> ActivateTemplateAsync(int id, int kullaniciId);
    Task<ResponseModel> DeactivateTemplateAsync(int id, int kullaniciId);
    Task<ResponseModel> DuplicateTemplateAsync(int id, int kullaniciId);
    Task<ResponseDataModel<TemplatePreviewDto>> PreviewTemplateAsync(int id, TemplatePreviewRequest? request);
    Task<ResponseModel> SaveAnnouncementAsTemplateAsync(int announcementId, string templateName, int kullaniciId);
}

public class TemplateService : ITemplateService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<TemplateService> _logger;
    private readonly IMapper _mapper;

    public TemplateService(DeuEpostaContext context, ILogger<TemplateService> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ResponseDataModel<List<EpostaSablon>>> GetTemplatesAsync()
    {
        try
        {
            var templates = await _context.EpostaSablonlari
                .Include(s => s.Kategori)
                .OrderByDescending(s => s.OlusturmaTarihi)
                .ToListAsync();

            return ResponseDataModel<List<EpostaSablon>>.SuccessResult(templates, "Şablonlar başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            return ResponseDataModel<List<EpostaSablon>>.ErrorResult("Şablonlar alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<EpostaSablon>>> GetActiveTemplatesAsync()
    {
        try
        {
            var templates = await _context.EpostaSablonlari
                .Include(s => s.Kategori)
                .Where(s => s.Aktif == "Y")
                .OrderBy(s => s.SablonAdi)
                .ToListAsync();

            return ResponseDataModel<List<EpostaSablon>>.SuccessResult(templates, "Aktif şablonlar başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active templates");
            return ResponseDataModel<List<EpostaSablon>>.ErrorResult("Aktif şablonlar alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<TemplateDetailView>> GetTemplateByIdAsync(int id)
    {
        try
        {
            var template = await _context.EpostaSablonlari
                .Include(s => s.Kategori)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (template == null)
                return ResponseDataModel<TemplateDetailView>.ErrorResult("Şablon bulunamadı", 404);

            var templateView = _mapper.Map<TemplateDetailView>(template);

            // Kullanım sayısını hesapla
            templateView.KullanimSayisi = await _context.EpostaDuyurulari
                .CountAsync(d => d.SablonId == id);

            return ResponseDataModel<TemplateDetailView>.SuccessResult(templateView, "Şablon başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {Id}", id);
            return ResponseDataModel<TemplateDetailView>.ErrorResult("Şablon alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> CreateTemplateAsync(CreateTemplateRequest request, int kullaniciId)
    {
        try
        {
            // Check if template name already exists
            var existingTemplate = await _context.EpostaSablonlari
                .FirstOrDefaultAsync(s => s.SablonAdi == request.Ad);

            if (existingTemplate != null)
                return ResponseModel.ErrorResult("Bu isimde bir şablon zaten var", 400);

            var template = _mapper.Map<EpostaSablon>(request);
            template.OlusturmaTarihi = DateTime.Now;
            template.Aktif = "Y";

            _context.EpostaSablonlari.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template created: {Id} - {Name} by user {UserId}", template.Id, template.SablonAdi, kullaniciId);
            
            return ResponseModel.SuccessResult("Şablon başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return ResponseModel.ErrorResult("Şablon oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> UpdateTemplateAsync(int id, UpdateTemplateRequest request, int kullaniciId)
    {
        try
        {
            var template = await _context.EpostaSablonlari.FirstOrDefaultAsync(s => s.Id == id);
            
            if (template == null)
                return ResponseModel.ErrorResult("Şablon bulunamadı", 404);

            // Check if another template with same name exists
            var existingTemplate = await _context.EpostaSablonlari
                .FirstOrDefaultAsync(s => s.SablonAdi == request.Ad && s.Id != id);

            if (existingTemplate != null)
                return ResponseModel.ErrorResult("Bu isimde başka bir şablon zaten var", 400);

            _mapper.Map(request, template);
            template.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template updated: {Id} - {Name} by user {UserId}", id, template.SablonAdi, kullaniciId);
            
            return ResponseModel.SuccessResult("Şablon başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {Id}", id);
            return ResponseModel.ErrorResult("Şablon güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteTemplateAsync(int id, int kullaniciId)
    {
        try
        {
            var template = await _context.EpostaSablonlari.FirstOrDefaultAsync(s => s.Id == id);
            
            if (template == null)
                return ResponseModel.ErrorResult("Şablon bulunamadı", 404);

            // Check if template is being used - Oracle compatible check
            var usageCount = await _context.EpostaDuyurulari
                .CountAsync(d => d.SablonId == id);

            if (usageCount > 0)
                return ResponseModel.ErrorResult("Bu şablon kullanımda olduğu için silinemez", 400);

            _context.EpostaSablonlari.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template deleted: {Id} - {Name} by user {UserId}", id, template.SablonAdi, kullaniciId);
            
            return ResponseModel.SuccessResult("Şablon başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id}", id);
            return ResponseModel.ErrorResult("Şablon silinirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ActivateTemplateAsync(int id, int kullaniciId)
    {
        try
        {
            var template = await _context.EpostaSablonlari.FirstOrDefaultAsync(s => s.Id == id);
            
            if (template == null)
                return ResponseModel.ErrorResult("Şablon bulunamadı", 404);

            if (template.Aktif == "Y")
                return ResponseModel.ErrorResult("Şablon zaten aktif", 400);

            template.Aktif = "Y";
            template.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template activated: {Id} - {Name} by user {UserId}", id, template.SablonAdi, kullaniciId);
            
            return ResponseModel.SuccessResult("Şablon aktif edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating template {Id}", id);
            return ResponseModel.ErrorResult("Şablon aktif edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeactivateTemplateAsync(int id, int kullaniciId)
    {
        try
        {
            var template = await _context.EpostaSablonlari.FirstOrDefaultAsync(s => s.Id == id);

            if (template == null)
                return ResponseModel.ErrorResult("Şablon bulunamadı", 404);

            if (template.Aktif == "N")
                return ResponseModel.ErrorResult("Şablon zaten pasif", 400);

            template.Aktif = "N";
            template.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template deactivated: {Id} - {Name} by user {UserId}", id, template.SablonAdi, kullaniciId);

            return ResponseModel.SuccessResult("Şablon pasif edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating template {Id}", id);
            return ResponseModel.ErrorResult("Şablon pasif edilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DuplicateTemplateAsync(int id, int kullaniciId)
    {
        try
        {
            var template = await _context.EpostaSablonlari.FirstOrDefaultAsync(s => s.Id == id);

            if (template == null)
                return ResponseModel.ErrorResult("Şablon bulunamadı", 404);

            // Create a copy with "(Kopya)" suffix
            var newTemplate = new EpostaSablon
            {
                SablonAdi = $"{template.SablonAdi} (Kopya)",
                KonuSablonu = template.KonuSablonu,
                IcerikSablonu = template.IcerikSablonu,
                KategoriId = template.KategoriId, // Copy category
                Varsayilan = "N", // Copy cannot be default
                Aktif = "Y",
                OlusturmaTarihi = DateTime.Now
            };

            _context.EpostaSablonlari.Add(newTemplate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template duplicated: {SourceId} -> {NewId} by user {UserId}",
                id, newTemplate.Id, kullaniciId);

            return ResponseModel.SuccessResult($"Şablon başarıyla çoğaltıldı. Yeni şablon ID: {newTemplate.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating template {Id}", id);
            return ResponseModel.ErrorResult("Şablon çoğaltılırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<TemplatePreviewDto>> PreviewTemplateAsync(int id, TemplatePreviewRequest? request)
    {
        try
        {
            var template = await _context.EpostaSablonlari.FirstOrDefaultAsync(s => s.Id == id);

            if (template == null)
                return ResponseDataModel<TemplatePreviewDto>.ErrorResult("Şablon bulunamadı", 404);

            // Default test data
            var testData = request ?? new TemplatePreviewRequest
            {
                Konu = "Örnek Duyuru Başlığı",
                Icerik = "<p>Bu bir örnek içeriktir. Şablonunuz bu şekilde görünecektir.</p>",
                Gonderen = "Test Kullanıcı",
                Tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };

            // Replace template variables
            var renderedSubject = ReplaceTemplateVariables(template.KonuSablonu ?? "", testData);
            var renderedContent = ReplaceTemplateVariables(template.IcerikSablonu, testData);

            var preview = new TemplatePreviewDto
            {
                TemplateId = template.Id,
                TemplateName = template.SablonAdi,
                RenderedSubject = renderedSubject,
                RenderedContent = renderedContent
            };

            return ResponseDataModel<TemplatePreviewDto>.SuccessResult(preview, "Şablon önizlemesi hazırlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing template {Id}", id);
            return ResponseDataModel<TemplatePreviewDto>.ErrorResult("Şablon önizlenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> SaveAnnouncementAsTemplateAsync(int announcementId, string templateName, int kullaniciId)
    {
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .FirstOrDefaultAsync(d => d.Id == announcementId);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // Check if template name already exists
            var existingTemplate = await _context.EpostaSablonlari
                .FirstOrDefaultAsync(s => s.SablonAdi == templateName);

            if (existingTemplate != null)
                return ResponseModel.ErrorResult("Bu isimde bir şablon zaten var", 400);

            // Create template from announcement
            var template = new EpostaSablon
            {
                SablonAdi = templateName,
                KonuSablonu = announcement.Konu,
                IcerikSablonu = announcement.Icerik,
                Varsayilan = "N",
                Aktif = "Y",
                OlusturmaTarihi = DateTime.Now
            };

            _context.EpostaSablonlari.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {AnnouncementId} saved as template {TemplateId} - {Name} by user {UserId}",
                announcementId, template.Id, template.SablonAdi, kullaniciId);

            return ResponseModel.SuccessResult($"Duyuru şablon olarak kaydedildi. Şablon ID: {template.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving announcement {Id} as template", announcementId);
            return ResponseModel.ErrorResult("Duyuru şablon olarak kaydedilirken hata oluştu", 500);
        }
    }

    private string ReplaceTemplateVariables(string template, TemplatePreviewRequest data)
    {
        return template
            .Replace("{{konu}}", data.Konu)
            .Replace("{{icerik}}", data.Icerik)
            .Replace("{{gonderen}}", data.Gonderen)
            .Replace("{{tarih}}", data.Tarih)
            .Replace("{{KONU}}", data.Konu) // Case variations
            .Replace("{{ICERIK}}", data.Icerik)
            .Replace("{{GONDEREN}}", data.Gonderen)
            .Replace("{{TARIH}}", data.Tarih);
    }
}

public class CreateTemplateRequest
{
    [Required(ErrorMessage = "Ad zorunludur")]
    public string Ad { get; set; } = string.Empty;
    
    public string Aciklama { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Başlık zorunludur")]
    public string Konu { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "İçerik zorunludur")]
    [SafeHtml(ErrorMessage = "İçerik güvenli HTML formatında olmalıdır")]
    public string Icerik { get; set; } = string.Empty;

    public string Kategori { get; set; } = string.Empty;

    public int? KategoriId { get; set; }
}

public class UpdateTemplateRequest
{
    [Required(ErrorMessage = "Ad zorunludur")]
    public string Ad { get; set; } = string.Empty;

    public string Aciklama { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlık zorunludur")]
    public string Konu { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik zorunludur")]
    [SafeHtml(ErrorMessage = "İçerik güvenli HTML formatında olmalıdır")]
    public string Icerik { get; set; } = string.Empty;

    public string Kategori { get; set; } = string.Empty;

    public int? KategoriId { get; set; }
}

public class TemplatePreviewRequest
{
    public string Konu { get; set; } = "Örnek Duyuru";
    public string Icerik { get; set; } = "<p>Örnek içerik</p>";
    public string Gonderen { get; set; } = "Sistem";
    public string Tarih { get; set; } = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
}

public class TemplatePreviewDto
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string RenderedSubject { get; set; } = string.Empty;
    public string RenderedContent { get; set; } = string.Empty;
}