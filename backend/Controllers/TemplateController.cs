using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize]
public class TemplateController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        ITemplateService templateService,
        ILogger<TemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm şablonları listele
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseDataModel<List<EpostaSablon>>>> GetTemplates()
    {
        var result = await _templateService.GetTemplatesAsync();

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Şablon detayını getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDataModel<TemplateDetailView>>> GetTemplate(int id)
    {
        var result = await _templateService.GetTemplateByIdAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Yeni şablon oluştur (ADMIN/MANAGER/COORDINATOR)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> CreateTemplate(CreateTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.CreateTemplateAsync(request, kullaniciId);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, ResponseModel.ErrorResult("Şablon oluşturulurken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Şablon güncelle (ADMIN/MANAGER/COORDINATOR)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> UpdateTemplate(int id, UpdateTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.UpdateTemplateAsync(id, request, kullaniciId);

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
            _logger.LogError(ex, "Error updating template {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Şablon güncellenirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Şablon sil (ADMIN/MANAGER/COORDINATOR)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> DeleteTemplate(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.DeleteTemplateAsync(id, kullaniciId);

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
            _logger.LogError(ex, "Error deleting template {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Şablon silinirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Şablonu aktif et (ADMIN/MANAGER/COORDINATOR)
    /// </summary>
    [HttpPut("{id}/activate")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> ActivateTemplate(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.ActivateTemplateAsync(id, kullaniciId);

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
            _logger.LogError(ex, "Error activating template {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Şablon aktif edilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Şablonu pasif et (ADMIN/MANAGER/COORDINATOR)
    /// </summary>
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> DeactivateTemplate(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.DeactivateTemplateAsync(id, kullaniciId);

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
            _logger.LogError(ex, "Error deactivating template {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Şablon pasif edilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Şablonu çoğalt (ADMIN/MANAGER/COORDINATOR)
    /// </summary>
    [HttpPost("{id}/duplicate")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> DuplicateTemplate(int id)
    {
        try
        {
            var kullaniciId = GetCurrentUserId();
            var result = await _templateService.DuplicateTemplateAsync(id, kullaniciId);

            return result.StatusCode switch
            {
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating template {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Şablon çoğaltılırken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Aktif şablonları listele (Duyuru oluşturmada kullanılacak)
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ResponseDataModel<List<EpostaSablon>>>> GetActiveTemplates()
    {
        var result = await _templateService.GetActiveTemplatesAsync();

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Şablon önizlemesi (test verisi ile render et)
    /// </summary>
    [HttpPost("{id}/preview")]
    public async Task<ActionResult<ResponseDataModel<TemplatePreviewDto>>> PreviewTemplate(int id, [FromBody] TemplatePreviewRequest? request)
    {
        try
        {
            var result = await _templateService.PreviewTemplateAsync(id, request);

            return result.StatusCode switch
            {
                404 => NotFound(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing template {Id}", id);
            return StatusCode(500, ResponseDataModel<TemplatePreviewDto>.ErrorResult("Şablon önizlenirken hata oluştu", 500));
        }
    }

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);
}