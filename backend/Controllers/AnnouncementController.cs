using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/announcements")]
[Authorize]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;
    private readonly IAnnouncementOperationsService _operationsService;
    private readonly IAnnouncementApprovalService _approvalService;
    private readonly IAnnouncementRecipientService _recipientService;
    private readonly ITemplateService _templateService;
    private readonly IEmailCategoryService _emailCategoryService;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<AnnouncementController> _logger;

    public AnnouncementController(
        IAnnouncementService announcementService,
        IAnnouncementOperationsService operationsService,
        IAnnouncementApprovalService approvalService,
        IAnnouncementRecipientService recipientService,
        ITemplateService templateService,
        IEmailCategoryService emailCategoryService,
        ISystemSettingsService systemSettingsService,
        IScheduleService scheduleService,
        ILogger<AnnouncementController> logger)
    {
        _announcementService = announcementService;
        _operationsService = operationsService;
        _approvalService = approvalService;
        _recipientService = recipientService;
        _templateService = templateService;
        _emailCategoryService = emailCategoryService;
        _systemSettingsService = systemSettingsService;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] int? kullaniciId = null)
    {
        var response = await _operationsService.GetAnnouncementStatisticsAsync(kullaniciId);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? durum = null,
        [FromQuery] bool onlyMine = false,
        [FromQuery] string? searchTerm = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var currentUserId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? RolKodu.VIEWER;
        var kullaniciId = onlyMine ? (int?)currentUserId : null;

        var response = await _announcementService.GetAnnouncementsAsync(
            page, pageSize, durum, kullaniciId, currentUserId, userRole,
            searchTerm, startDate, endDate);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDataModel<AnnouncementDetailView>>> GetAnnouncement(int id)
    {
        var result = await _announcementService.GetAnnouncementByIdAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        // GÜVENLIK: IDOR prevention - Authorization kontrolü
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole(RolKodu.ADMIN);
        var isManager = User.IsInRole(RolKodu.MANAGER);
        var isCoordinator = User.IsInRole(RolKodu.COORDINATOR);
        var isViewer = User.IsInRole(RolKodu.VIEWER);

        // ADMIN, MANAGER, COORDINATOR, VIEWER: Tüm duyuruları görebilir (read-only)
        // EDITOR: Sadece kendi oluşturduğu duyuruları görebilir
        if (!isAdmin && !isManager && !isCoordinator && !isViewer)
        {
            var announcement = result.Data;
            if (announcement == null)
                return NotFound(ResponseModel.ErrorResult("Duyuru bulunamadı", 404));

            bool isOwner = announcement.OlusturanKullaniciId == currentUserId;

            if (!isOwner)
            {
                return StatusCode(403, ResponseModel.ErrorResult("Bu duyuruya erişim yetkiniz yok", 403));
            }
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<ActionResult<ResponseModel>> CreateAnnouncement(CreateAnnouncementRequest request)
    {        

        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _announcementService.CreateAnnouncementAsync(request, kullaniciId);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                401 => Unauthorized(result),
                403 => Forbid(),
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating announcement");
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru oluşturulurken hata oluştu", 500));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<ActionResult<ResponseModel>> UpdateAnnouncement(int id, UpdateAnnouncementRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _announcementService.UpdateAnnouncementAsync(id, request, kullaniciId);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                401 => Unauthorized(result),
                403 => Forbid(),
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru güncellenirken hata oluştu", 500));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<ActionResult<ResponseModel>> DeleteAnnouncement(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _announcementService.DeleteAnnouncementAsync(id, kullaniciId);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                401 => Unauthorized(result),
                403 => Forbid(),
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru silinirken hata oluştu", 500));
        }
    }

    [HttpPost("{id}/duplicate")]
    [Authorize(Roles = "ADMIN,EDITOR")]
    public async Task<ActionResult<ResponseModel>> DuplicateAnnouncement(int id)
    {
        var kullaniciId = GetCurrentUserId();
        var result = await _announcementService.DuplicateAnnouncementAsync(id, kullaniciId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// İptal edilmiş duyuruyu yeniden aktif eder (ONAYLANDI durumuna döndürür)
    /// </summary>
    [HttpPut("{id}/reactivate")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<ActionResult<ResponseModel>> ReactivateAnnouncement(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();            
            var result = await _operationsService.ReactivateAnnouncementAsync(id, kullaniciId);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru yeniden aktif edilirken hata oluştu", 500));
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<ActionResult<ResponseModel>> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var kullaniciId = GetCurrentUserId();
        var result = await _operationsService.ChangeStatusAsync(id, request.YeniDurum, kullaniciId, request.Note);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("categories")]
    [Authorize]
    public async Task<IActionResult> GetAnnouncementCategories()
    {
        try
        {
            // Kullanıcının rol ve görev yeri bilgisini al (JWT claims)
            var rolKodu = User.FindFirst("RolKodu")?.Value;
            var gorevYeriClaim = User.FindFirst("GorevYeri")?.Value;
            int? gorevYeri = int.TryParse(gorevYeriClaim, out var gv) ? gv : null;

            // YENİ YAPI: SystemSettingsService kullan (EMAIL_KATEGORI tek tablo)
            var categories = await _systemSettingsService.GetEmailCategoriesAsync(rolKodu, gorevYeri);
            return Ok(ResponseDataModel<List<EmailCategoryDto>>.SuccessResult(categories, "Duyuru kategorileri alındı"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement categories");
            return StatusCode(500, ResponseModel.ErrorResult("Kategoriler alınırken hata oluştu", 500));
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<ResponseDataModel<List<EpostaSablon>>>> GetTemplates()
    {
        var result = await _templateService.GetTemplatesAsync();

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<ResponseDataModel<TemplateDetailView>>> GetTemplate(int id)
    {
        var result = await _templateService.GetTemplateByIdAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("{id}/send-logs")]
    public async Task<ActionResult<ResponseDataModel<List<EpostaDuyuruGonderimLog>>>> GetSendLogs(int id)
    {
        var result = await _operationsService.GetSendLogsAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("{id}/sent-count")]
    public async Task<ActionResult<ResponseDataModel<int>>> GetSentRecipientCount(int id)
    {
        var result = await _recipientService.GetSentRecipientCountAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("{id}/schedule")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<ActionResult<ResponseModel>> ScheduleAnnouncement(int id, ScheduleAnnouncementRequest request)
    {
        var kullaniciId = GetCurrentUserId();

        var scheduleRequest = new CreateScheduleRequest
        {
            DuyuruId = id,
            ZamanlanmaTarihi = request.ScheduledDate
        };

        var result = await _scheduleService.CreateScheduleAsync(scheduleRequest, kullaniciId);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("{id}/preview")]
    public async Task<IActionResult> GetAnnouncementPreview(int id)
    {
        var response = await _operationsService.GetAnnouncementPreviewAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Test email gönder - Duyuruyu önizlemek için
    /// </summary>
    [HttpPost("{id}/send-test")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR,EDITOR")]
    public async Task<IActionResult> SendTestEmail(int id, [FromBody] SendTestEmailRequest? request)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _operationsService.SendTestEmailAsync(id, kullaniciId, request?.TestEmail);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                403 => Forbid(),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email for announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Test email gönderilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Onaylanmış duyuruyu gönderime alma
    /// </summary>
    [HttpPost("{id}/send")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<IActionResult> SendAnnouncement(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();

            var result = await _approvalService.SendAnnouncementAsync(id, kullaniciId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                403 => Forbid(),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru gönderimi sırasında hata oluştu", 500));
        }
    }

    /// <summary>
    /// ADMIN/MANAGER için: Duyuruyu onayla ve direkt gönder (onay süreci atlanır, bildirim gönderilmez)
    /// </summary>
    [HttpPost("{id}/approve-and-send")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> ApproveAndSendAnnouncement(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();            

            var result = await _approvalService.ApproveAndSendAnnouncementAsync(id, kullaniciId);

            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                403 => Forbid(),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving and sending announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru onaylanıp gönderilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Duyuruyu şablon olarak kaydet
    /// </summary>
    [HttpPost("{id}/save-as-template")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<ActionResult<ResponseModel>> SaveAnnouncementAsTemplate(int id, [FromBody] SaveAsTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.SaveAnnouncementAsTemplateAsync(id, request.TemplateName, kullaniciId);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving announcement {Id} as template", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru şablon olarak kaydedilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Duyuru alıcılarını getir (Frontend uyumlu endpoint)
    /// </summary>
    [HttpGet("{id}/recipients")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<IActionResult> GetAnnouncementRecipients(int id)
    {
        var response = await _recipientService.GetRecipientsAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Duyuru geçmişini (hareketlerini) getir
    /// </summary>
    [HttpGet("{id}/movements")]
    public async Task<IActionResult> GetAnnouncementMovements(int id)
    {
        try
        {
            var response = await _operationsService.GetAnnouncementMovementsAsync(id);

            return response.StatusCode switch
            {
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => Ok(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movements for announcement {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Duyuru geçmişi alınırken hata oluştu", 500));
        }
    }

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);
   
}
