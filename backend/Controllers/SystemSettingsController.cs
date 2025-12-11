using DeuEposta.Jobs;
using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/admin/system-settings")]
[Authorize]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IEmailCategoryService _emailCategoryService;
    private readonly OrphanFileCleanupJob _orphanFileCleanupJob;

    public SystemSettingsController(
        ISystemSettingsService systemSettingsService,
        IEmailCategoryService emailCategoryService,
        OrphanFileCleanupJob orphanFileCleanupJob)
    {
        _systemSettingsService = systemSettingsService;
        _emailCategoryService = emailCategoryService;
        _orphanFileCleanupJob = orphanFileCleanupJob;
    }

    [HttpGet("email-settings")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetEmailSettingsList()
    {
        var response = await _systemSettingsService.GetEmailSettingsAsync();

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPut("email-settings")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateEmailSettings(UpdateEmailSettingsRequest request)
    {
        var response = await _systemSettingsService.UpdateEmailSettingsAsync(request);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("all")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllSettings(
        [FromQuery] string? category = null,
        [FromQuery] bool includeSecret = false,
        [FromQuery] bool includeInactive = false)
    {
        var response = await _systemSettingsService.GetAllSettingsAsync(category, includeSecret, includeInactive);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // REMOVED: Duplicate endpoint - UserController.GetApprovers() kullanılıyor
    // Frontend: GET /api/User/approvers (user.service.ts:110, 195)

    // REMOVED: Duplicate endpoint - AnnouncementController.GetAnnouncementCategories() kullanılıyor
    // Frontend: GET /api/announcements/categories (announcement.service.ts:30)

    /// <summary>
    /// SMTP gönderici kategorilerini getirir (EMAIL_DUYURU, EMAIL_REKTOR, vb.)
    /// Frontend dropdown için kullanılır
    /// </summary>
    [HttpGet("smtp-categories")]
    [Authorize(Roles = "ADMIN,EDITOR,COORDINATOR,MANAGER")]
    public async Task<IActionResult> GetSmtpCategories()
    {
        var smtpCategories = await _emailCategoryService.GetActiveEmailCategoriesAsync();

        // Her kategori için FROM_EMAIL bilgisini al
        var categoriesWithDetails = new List<object>();
        foreach (var category in smtpCategories)
        {
            var config = await _emailCategoryService.GetEmailConfigByCategoryAsync(category);
            var displayName = await _emailCategoryService.GetCategoryDisplayNameAsync(category);

            categoriesWithDetails.Add(new
            {
                key = category,
                displayName = displayName,
                email = config.FromEmail
            });
        }

        return Ok(new { success = true, data = categoriesWithDetails, message = "SMTP gönderici kategorileri alındı" });
    }

    /// <summary>
    /// SMTP bağlantı testi yapar
    /// </summary>
    [HttpPost("test-smtp-connection")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> TestSmtpConnection([FromBody] string category)
    {
        // Login olan admin kullanıcısının email'ini al
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        var response = await _systemSettingsService.TestSmtpConnectionAsync(category, userEmail);
        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateSetting(CreateSystemSettingRequest request)
    {
        var response = await _systemSettingsService.CreateSettingAsync(request);
        return response.StatusCode switch
        {
            400 => BadRequest(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPut("bulk")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> BulkUpdateSettings(List<BulkUpdateSettingRequest> requests)
    {
        var response = await _systemSettingsService.BulkUpdateSettingsAsync(requests);
        return response.StatusCode switch
        {
            400 => BadRequest(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateSetting(int id, UpdateSystemSettingRequest request)
    {
        var response = await _systemSettingsService.UpdateSettingAsync(id, request);
        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteSetting(int id)
    {
        var response = await _systemSettingsService.DeleteSettingAsync(id);
        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// ADMIN ONLY: Orphan dosya temizleme job'unu manuel olarak tetikler (test amaçlı)
    /// Normalde her ayın 1'i gece 00:00'da otomatik çalışır
    /// </summary>
    [HttpPost("cleanup-orphan-files")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> TriggerOrphanFileCleanup()
    {
        try
        {
            await _orphanFileCleanupJob.CleanupOrphanFilesAsync();
            return Ok(new { success = true, message = "Orphan dosya temizleme işlemi başarıyla tamamlandı. Detaylar için loglara bakın." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Temizleme işlemi başarısız", error = ex.Message });
        }
    }
}