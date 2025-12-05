using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Models.DTOs;

public class CreateTemplateCategoryRequest
{
    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir")]
    public string KategoriAdi { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get; set; }

    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Renk hex formatında olmalıdır (örn: #1976d2)")]
    public string? Renk { get; set; }

    [StringLength(50, ErrorMessage = "İkon adı en fazla 50 karakter olabilir")]
    public string? Ikon { get; set; }

    public int SiraNo { get; set; } = 0;
}

public class UpdateTemplateCategoryRequest
{
    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir")]
    public string KategoriAdi { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Aciklama { get; set; }

    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Renk hex formatında olmalıdır (örn: #1976d2)")]
    public string? Renk { get; set; }

    [StringLength(50, ErrorMessage = "İkon adı en fazla 50 karakter olabilir")]
    public string? Ikon { get; set; }

    public int SiraNo { get; set; } = 0;
}

public class ReorderCategoriesRequest
{
    [Required(ErrorMessage = "Kategori ID listesi zorunludur")]
    [MinLength(1, ErrorMessage = "En az bir kategori ID'si gereklidir")]
    public List<int> CategoryIds { get; set; } = new();
}