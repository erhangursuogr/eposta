using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeuEposta.Controllers;

/// <summary>
/// Duyuru zamanlama yönetimi controller'ı
/// Onaylanmış duyuruların zamanlanmış gönderimlerini yönetir
/// </summary>
[ApiController]
[Route("api/schedules")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        IScheduleService scheduleService,
        ILogger<ScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Tek bir zamanlama oluştur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<ActionResult<ResponseDataModel<ScheduleResponse>>> CreateSchedule(
        [FromBody] CreateScheduleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _scheduleService.CreateScheduleAsync(request, kullaniciId);

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
            _logger.LogError(ex, "Error creating schedule for announcement {DuyuruId}", request.DuyuruId);
            return StatusCode(500, ResponseModel.ErrorResult("Zamanlama oluşturulurken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Toplu zamanlama oluştur (tekrarlı gönderimler)
    /// Örn: 1 ay boyunca her 5 günde bir
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<ActionResult<ResponseDataModel<List<ScheduleResponse>>>> CreateBulkSchedule(
        [FromBody] CreateBulkScheduleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _scheduleService.CreateBulkScheduleAsync(request, kullaniciId);

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
            _logger.LogError(ex, "Error creating bulk schedules for announcement {DuyuruId}", request.DuyuruId);
            return StatusCode(500, ResponseModel.ErrorResult("Toplu zamanlama oluşturulurken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Tüm zamanlamaları getir (filtrelenebilir)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseDataModel<List<ScheduleResponse>>>> GetAllSchedules(
        [FromQuery] string? durum = null)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var result = await _scheduleService.GetAllSchedulesAsync(kullaniciId, userRole, durum);

            return result.StatusCode switch
            {
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all schedules");
            return StatusCode(500, ResponseModel.ErrorResult("Zamanlamalar getirilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Belirli bir duyuruya ait tüm zamanlamaları getir
    /// </summary>
    [HttpGet("announcement/{duyuruId}")]
    public async Task<ActionResult<ResponseDataModel<List<ScheduleResponse>>>> GetSchedulesForAnnouncement(int duyuruId)
    {
        try
        {
            var result = await _scheduleService.GetSchedulesForAnnouncementAsync(duyuruId);

            return result.StatusCode switch
            {
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedules for announcement {DuyuruId}", duyuruId);
            return StatusCode(500, ResponseModel.ErrorResult("Zamanlamalar getirilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Zamanlamayı iptal et (Hangfire job'ını da siler)
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<ActionResult<ResponseModel>> CancelSchedule(
        int id,
        [FromBody] CancelScheduleRequest? request = null)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _scheduleService.CancelScheduleAsync(id, kullaniciId, request?.IptalNotu ?? "");

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
            _logger.LogError(ex, "Error cancelling schedule {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Zamanlama iptal edilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Zamanlamayı kalıcı olarak sil
    /// Sadece IPTAL veya HATA durumundaki zamanlamalar silinebilir
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseModel>> DeleteSchedule(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _scheduleService.DeleteScheduleAsync(id, kullaniciId);

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
            _logger.LogError(ex, "Error deleting schedule {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Zamanlama silinirken hata oluştu", 500));
        }
    }

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);

}