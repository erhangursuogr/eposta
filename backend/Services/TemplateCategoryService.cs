using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface ITemplateCategoryService
{
    Task<ResponseDataModel<List<EpostaSablonKategori>>> GetAllCategoriesAsync();
    Task<ResponseDataModel<List<EpostaSablonKategori>>> GetActiveCategoriesAsync();
    Task<ResponseDataModel<EpostaSablonKategori>> GetCategoryByIdAsync(int id);
    Task<ResponseDataModel<EpostaSablonKategori>> CreateCategoryAsync(CreateTemplateCategoryRequest request);
    Task<ResponseModel> UpdateCategoryAsync(int id, UpdateTemplateCategoryRequest request);
    Task<ResponseModel> DeleteCategoryAsync(int id);
    Task<ResponseModel> ActivateCategoryAsync(int id);
    Task<ResponseModel> DeactivateCategoryAsync(int id);
    Task<ResponseModel> ReorderCategoriesAsync(List<int> categoryIds);
}

public class TemplateCategoryService : ITemplateCategoryService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<TemplateCategoryService> _logger;

    public TemplateCategoryService(DeuEpostaContext context, ILogger<TemplateCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseDataModel<List<EpostaSablonKategori>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _context.EpostaSablonKategorileri
                .OrderBy(k => k.SiraNo)
                .ThenBy(k => k.KategoriAdi)
                .ToListAsync();

            return ResponseDataModel<List<EpostaSablonKategori>>.SuccessResult(categories, "Kategoriler başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template categories");
            return ResponseDataModel<List<EpostaSablonKategori>>.ErrorResult("Kategoriler alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<EpostaSablonKategori>>> GetActiveCategoriesAsync()
    {
        try
        {
            var categories = await _context.EpostaSablonKategorileri
                .Where(k => k.Aktif == "Y")
                .OrderBy(k => k.SiraNo)
                .ThenBy(k => k.KategoriAdi)
                .ToListAsync();

            return ResponseDataModel<List<EpostaSablonKategori>>.SuccessResult(categories, "Aktif kategoriler başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active template categories");
            return ResponseDataModel<List<EpostaSablonKategori>>.ErrorResult("Aktif kategoriler alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<EpostaSablonKategori>> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _context.EpostaSablonKategorileri
                .FirstOrDefaultAsync(k => k.Id == id);

            if (category == null)
                return ResponseDataModel<EpostaSablonKategori>.ErrorResult("Kategori bulunamadı", 404);

            return ResponseDataModel<EpostaSablonKategori>.SuccessResult(category, "Kategori başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template category {Id}", id);
            return ResponseDataModel<EpostaSablonKategori>.ErrorResult("Kategori alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<EpostaSablonKategori>> CreateCategoryAsync(CreateTemplateCategoryRequest request)
    {
        try
        {
            // Aynı isimde kategori var mı kontrol et
            var existingCategory = await _context.EpostaSablonKategorileri
                .FirstOrDefaultAsync(k => k.KategoriAdi == request.KategoriAdi);

            if (existingCategory != null)
                return ResponseDataModel<EpostaSablonKategori>.ErrorResult("Bu isimde bir kategori zaten mevcut", 400);

            var category = new EpostaSablonKategori
            {
                KategoriAdi = request.KategoriAdi,
                Aciklama = request.Aciklama,
                Renk = request.Renk ?? "#1976d2",
                Ikon = request.Ikon ?? "label",
                SiraNo = request.SiraNo,
                Aktif = "Y",
                OlusturmaTarihi = DateTime.Now
            };

            _context.EpostaSablonKategorileri.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template category created: {CategoryName} (ID: {Id})", category.KategoriAdi, category.Id);
            return ResponseDataModel<EpostaSablonKategori>.SuccessResult(category, "Kategori başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template category");
            return ResponseDataModel<EpostaSablonKategori>.ErrorResult("Kategori oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> UpdateCategoryAsync(int id, UpdateTemplateCategoryRequest request)
    {
        try
        {
            var category = await _context.EpostaSablonKategorileri
                .FirstOrDefaultAsync(k => k.Id == id);

            if (category == null)
                return ResponseModel.ErrorResult("Kategori bulunamadı", 404);

            // İsim değiştiyse, aynı isimde başka kategori var mı kontrol et
            if (category.KategoriAdi != request.KategoriAdi)
            {
                var existingCategory = await _context.EpostaSablonKategorileri
                    .FirstOrDefaultAsync(k => k.KategoriAdi == request.KategoriAdi && k.Id != id);

                if (existingCategory != null)
                    return ResponseModel.ErrorResult("Bu isimde bir kategori zaten mevcut", 400);
            }

            category.KategoriAdi = request.KategoriAdi;
            category.Aciklama = request.Aciklama;
            category.Renk = request.Renk ?? category.Renk;
            category.Ikon = request.Ikon ?? category.Ikon;
            category.SiraNo = request.SiraNo;
            category.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Template category updated: {CategoryName} (ID: {Id})", category.KategoriAdi, id);
            return ResponseModel.SuccessResult("Kategori başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template category {Id}", id);
            return ResponseModel.ErrorResult("Kategori güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _context.EpostaSablonKategorileri
                .FirstOrDefaultAsync(k => k.Id == id);

            if (category == null)
                return ResponseModel.ErrorResult("Kategori bulunamadı", 404);

            // Bu kategoriye ait şablon var mı kontrol et
            var templateCount = await _context.EpostaSablonlari
                .CountAsync(s => s.KategoriId == id);

            if (templateCount > 0)
                return ResponseModel.ErrorResult($"Bu kategoriye ait {templateCount} şablon bulunmaktadır. Önce şablonları silin veya kategorilerini değiştirin", 400);

            _context.EpostaSablonKategorileri.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template category deleted: {CategoryName} (ID: {Id})", category.KategoriAdi, id);
            return ResponseModel.SuccessResult("Kategori başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template category {Id}", id);
            return ResponseModel.ErrorResult("Kategori silinirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ActivateCategoryAsync(int id)
    {
        try
        {
            var category = await _context.EpostaSablonKategorileri
                .FirstOrDefaultAsync(k => k.Id == id);

            if (category == null)
                return ResponseModel.ErrorResult("Kategori bulunamadı", 404);

            if (category.Aktif == "Y")
                return ResponseModel.ErrorResult("Kategori zaten aktif", 400);

            category.Aktif = "Y";
            category.GuncellemeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template category activated: {CategoryName} (ID: {Id})", category.KategoriAdi, id);
            return ResponseModel.SuccessResult("Kategori başarıyla aktifleştirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating template category {Id}", id);
            return ResponseModel.ErrorResult("Kategori aktifleştirilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeactivateCategoryAsync(int id)
    {
        try
        {
            var category = await _context.EpostaSablonKategorileri
                .FirstOrDefaultAsync(k => k.Id == id);

            if (category == null)
                return ResponseModel.ErrorResult("Kategori bulunamadı", 404);

            if (category.Aktif == "N")
                return ResponseModel.ErrorResult("Kategori zaten pasif", 400);

            category.Aktif = "N";
            category.GuncellemeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template category deactivated: {CategoryName} (ID: {Id})", category.KategoriAdi, id);
            return ResponseModel.SuccessResult("Kategori başarıyla pasifleştirildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating template category {Id}", id);
            return ResponseModel.ErrorResult("Kategori pasifleştirilirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> ReorderCategoriesAsync(List<int> categoryIds)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // N+1 problemi önlendi: Tüm kategorileri tek sorguda al
                var categories = await _context.EpostaSablonKategorileri
                    .Where(k => categoryIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id);

                for (int i = 0; i < categoryIds.Count; i++)
                {
                    if (categories.TryGetValue(categoryIds[i], out var category))
                    {
                        category.SiraNo = i;
                        category.GuncellemeTarihi = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Template categories reordered");
                return ResponseModel.SuccessResult("Kategori sıralaması başarıyla güncellendi");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering template categories");
            return ResponseModel.ErrorResult("Kategori sıralaması güncellenirken hata oluştu", 500);
        }
    }
}
