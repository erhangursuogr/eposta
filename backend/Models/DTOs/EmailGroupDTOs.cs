using DeuEposta.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Models.DTOs;

/// <summary>
/// Email grubu oluşturma DTO
/// </summary>
public class CreateEmailGroupDto
{
    [Required]
    [StringLength(100)]
    public string GrupAdi { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [Required]
    public GrupTipi GrupTipi { get; set; }

    // Dinamik grup için
    [StringLength(100)]
    public string? ViewAdi { get; set; }

    [StringLength(500)]
    public string? FilterKosulu { get; set; }

    // DEBIS grup için listeci email adresi
    [StringLength(200)]
    public string? ListeciEmail { get; set; }

    // Statik grup için dosya yükleme
    public List<StatikUyeDto>? StatikUyeler { get; set; }
}

/// <summary>
/// Email grubu güncelleme DTO
/// </summary>
public class UpdateEmailGroupDto
{
    [StringLength(100)]
    public string? GrupAdi { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [StringLength(500)]
    public string? FilterKosulu { get; set; }

    // DEBIS grup için listeci email adresi
    [StringLength(200)]
    public string? ListeciEmail { get; set; }

    public List<StatikUyeDto>? StatikUyeler { get; set; }

    // Aktif/Pasif durumu (sadece ADMIN değiştirebilir)
    [StringLength(1)]
    public string? Aktif { get; set; }
}

/// <summary>
/// Email grubu detay DTO
/// </summary>
public class EmailGroupDetailDto
{
    public int Id { get; set; }
    public string GrupAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public GrupTipi GrupTipi { get; set; }
    public string GrupTipiText => GrupTipi.GetDescription();

    // Dinamik grup bilgileri
    public string? ViewAdi { get; set; }

    public string? FilterKosulu { get; set; }

    // DEBIS grup bilgileri - ListeciEmail frontend'e gönderilmiyor (güvenlik - backend'de default: listeci@deu.edu.tr)

    // Kural bilgileri
    public bool BccOnly => GrupTipi.IsBccOnly();

    public bool CanUseCC => !GrupTipi.IsBccOnly();

    public string Aktif { get; set; } = "Y";
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }

    // Üye sayısı
    public int UyeSayisi { get; set; }

    // Üyeler (sadece gerektiğinde)
    public List<EmailGroupMemberDto>? Uyeler { get; set; }
}

/// <summary>
/// Email grubu liste DTO
/// </summary>
public class EmailGroupListDto
{
    public int Id { get; set; }
    public string GrupAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public GrupTipi GrupTipi { get; set; }
    public string GrupTipiText => GrupTipi.GetDescription();
    public bool BccOnly => GrupTipi.IsBccOnly();
    public int UyeSayisi { get; set; }
    public string Aktif { get; set; } = "Y";
    public bool IsActive => Aktif == "Y"; // Computed property for convenience
    public DateTime OlusturmaTarihi { get; set; }
}

/// <summary>
/// Email grubu üye DTO
/// </summary>
public class EmailGroupMemberDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AdSoyad { get; set; }
    public string? Departman { get; set; }
    public string Aktif { get; set; } = "Y";
    public bool IsActive => Aktif == "AKTIF"; // Computed property for member status
    public DateTime EklenmeTarihi { get; set; }
}

/// <summary>
/// Statik grup üye DTO
/// </summary>
public class StatikUyeDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? AdSoyad { get; set; }

    [StringLength(100)]
    public string? Departman { get; set; }
}

/// <summary>
/// Sistem email adresi DTO (FROM, TO için)
/// </summary>
public class SystemEmailAddressDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
}

/// <summary>
/// Dinamik grup önizleme DTO
/// </summary>
public class DynamicGroupPreviewDto
{
    public string ViewAdi { get; set; } = string.Empty;
    public string? FilterKosulu { get; set; }
    public int ToplamUye { get; set; }
    public List<EmailGroupMemberDto> OnizlemeUyeler { get; set; } = new();
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dosyadan üye import sonucu
/// </summary>
public class ImportMembersResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public List<string> FailedEmails { get; set; } = new();
    public List<string> DuplicateEmails { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Dinamik grup önizleme isteği
/// </summary>
public class PreviewDynamicGroupRequest
{
    [Required]
    [StringLength(100)]
    public string ViewAdi { get; set; } = string.Empty;

    [StringLength(500)]
    public string? FilterKosulu { get; set; }
}

/// <summary>
/// Gruba üye ekleme isteği
/// </summary>
public class AddGroupMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = string.Empty;
}

/// <summary>
/// Grup üyesi güncelleme isteği
/// </summary>
public class UpdateGroupMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = string.Empty;

    [Required]
    public string Durum { get; set; } = "AKTIF"; // AKTIF, PASIF
}