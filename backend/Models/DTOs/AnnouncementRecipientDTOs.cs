using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Models.DTOs;

public class AddManualRecipientRequest
{
    [Required(ErrorMessage = "Email adresi zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(254, ErrorMessage = "Email adresi çok uzun")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad Soyad 2-100 karakter arasında olmalıdır")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alıcı kategorisi zorunludur")]
    [RegularExpression("^(TO|CC|BCC)$", ErrorMessage = "Geçerli kategoriler: TO, CC, BCC")]
    public string Kategori { get; set; } = "TO"; // TO, CC, BCC
}

public class RecipientStatsDto
{
    public int TotalCount { get; set; }
    public int ToCount { get; set; }
    public int CcCount { get; set; }
    public int BccCount { get; set; }
    public int GroupCount { get; set; }
    public int ManualCount { get; set; }
}