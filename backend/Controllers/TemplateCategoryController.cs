using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/template-categories")]
[Authorize]
[EnableRateLimiting("Api")]
public class TemplateCategoryController : ControllerBase
{
    private readonly ITemplateCategoryService _categoryService;
    private readonly ILogger<TemplateCategoryController> _logger;

    public TemplateCategoryController(
        ITemplateCategoryService categoryService,
        ILogger<TemplateCategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm kategorileri listele
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseDataModel<List<EpostaSablonKategori>>>> GetAllCategories()
    {
        var result = await _categoryService.GetAllCategoriesAsync();

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Aktif kategorileri listele
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ResponseDataModel<List<EpostaSablonKategori>>>> GetActiveCategories()
    {
        var result = await _categoryService.GetActiveCategoriesAsync();

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Kategori detayını getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDataModel<EpostaSablonKategori>>> GetCategory(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Yeni kategori oluştur (ADMIN, COORDINATOR)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<ActionResult<ResponseDataModel<EpostaSablonKategori>>> CreateCategory(CreateTemplateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDataModel<EpostaSablonKategori>.ErrorResult("Geçersiz model verisi", 400));

            var result = await _categoryService.CreateCategoryAsync(request);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template category");
            return StatusCode(500, ResponseDataModel<EpostaSablonKategori>.ErrorResult("Kategori oluşturulurken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Kategori güncelle (ADMIN, COORDINATOR)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> UpdateCategory(int id, UpdateTemplateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var result = await _categoryService.UpdateCategoryAsync(id, request);

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
            _logger.LogError(ex, "Error updating template category {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Kategori güncellenirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Kategori sil (ADMIN, COORDINATOR)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> DeleteCategory(int id)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id);

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
            _logger.LogError(ex, "Error deleting template category {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Kategori silinirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Kategoriyi aktifleştir (ADMIN, COORDINATOR)
    /// </summary>
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> ActivateCategory(int id)
    {
        try
        {
            var result = await _categoryService.ActivateCategoryAsync(id);

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
            _logger.LogError(ex, "Error activating template category {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Kategori aktifleştirilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Kategoriyi pasifleştir (ADMIN, COORDINATOR)
    /// </summary>
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> DeactivateCategory(int id)
    {
        try
        {
            var result = await _categoryService.DeactivateCategoryAsync(id);

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
            _logger.LogError(ex, "Error deactivating template category {Id}", id);
            return StatusCode(500, ResponseModel.ErrorResult("Kategori pasifleştirilirken hata oluştu", 500));
        }
    }

    /// <summary>
    /// Kategori sıralamasını güncelle (ADMIN, COORDINATOR)
    /// </summary>
    [HttpPost("reorder")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<ActionResult<ResponseModel>> ReorderCategories(ReorderCategoriesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseModel.ErrorResult("Geçersiz model verisi", 400));

            var result = await _categoryService.ReorderCategoriesAsync(request.CategoryIds);

            return result.StatusCode switch
            {
                400 => BadRequest(result),
                500 => StatusCode(500, result),
                _ => Ok(result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering template categories");
            return StatusCode(500, ResponseModel.ErrorResult("Kategori sıralaması güncellenirken hata oluştu", 500));
        }
    }
}