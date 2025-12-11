using DeuEposta.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeuEposta.Models.DTOs;

public class CreateAnnouncementRequest
{
    /// <summary>
    /// Konu - Email subject ve display title olarak kullanılır
    /// </summary>
    [Required(ErrorMessage = "Konu zorunludur")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Konu 5-500 karakter arasında olmalıdır")]
    public string Konu { get; set; } = string.Empty;

    /// <summary>
    /// İçerik (Opsiyonel - Şablon seçiliyse şablondan alınır)
    /// </summary>
    [MinLength(10, ErrorMessage = "İçerik en az 10 karakter olmalıdır")]
    [SafeHtml(ErrorMessage = "İçerik güvenli HTML formatında olmalıdır")]
    public string? Icerik { get; set; }

    /// <summary>
    /// Şablon ID (Opsiyonel - Seçilirse şablon konu ve içeriği duyuruya yüklenir)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz")]
    public int? SablonId { get; set; }

    /// <summary>
    /// Banner/Logo Dosya ID (Opsiyonel - Email üstünde görünecek görsel)
    /// </summary>
    public int? BannerDosyaId { get; set; }

    /// <summary>
    /// Duyuru imza kategorisi (GENEL_DUYURU_IMZASIZ, REKTOR, BID, vb.) - EMAIL_IMZA tablosundan
    /// </summary>
    [Required(ErrorMessage = "Duyuru kategorisi zorunludur")]
    [StringLength(50, ErrorMessage = "Duyuru kategorisi en fazla 50 karakter olmalıdır")]
    public string DuyuruKategorisi { get; set; } = string.Empty;

    /// <summary>
    /// SMTP gönderici kategorisi (EMAIL_REKTOR, EMAIL_BID, EMAIL_DUYURU, vb.)
    /// Hangi email hesabından gönderileceğini belirler
    /// </summary>
    [Required(ErrorMessage = "Gönderici kategorisi zorunludur")]
    [StringLength(50, ErrorMessage = "Gönderici kategorisi en fazla 50 karakter olmalıdır")]
    public string GondericiKategori { get; set; } = "EMAIL_DUYURU";

    /// <summary>
    /// Duyuruyu onaylayacak kullanıcının ID'si (opsiyonel - MANAGER veya ADMIN rolünde olmalı)
    /// </summary>
    public int? OnaylayanKullaniciId { get; set; }

    /// <summary>
    /// Alıcı grup ID listesi
    /// </summary>
    public List<int> GrupIdList { get; set; } = new();

    /// <summary>
    /// Manuel alıcı email listesi
    /// </summary>
    public List<string> AliciEmailList { get; set; } = new();

    /// <summary>
    /// Duyuruya eklenecek dosya ID listesi
    /// </summary>
    public List<int> DosyaIdList { get; set; } = new();

    /// <summary>
    /// Zamanlanmış gönderim tarihi (opsiyonel)
    /// </summary>
    public DateTime? ZamanlanmisTarih { get; set; }

    /// <summary>
    /// Tekrarlama sıklığı (NONE, DAILY, WEEKLY, MONTHLY)
    /// </summary>
    public string? TekrarSikligi { get; set; }

    /// <summary>
    /// Tekrarlama bitiş tarihi
    /// </summary>
    public DateTime? TekrarBitisTarihi { get; set; }

    /// <summary>
    /// Duyuru açıklaması (opsiyonel)
    /// </summary>
    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olmalıdır")]
    public string? Aciklama { get; set; }
}

public class UpdateAnnouncementRequest
{
    [Required(ErrorMessage = "Konu zorunludur")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Konu 5-500 karakter arasında olmalıdır")]
    public string Konu { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik zorunludur")]
    [MinLength(10, ErrorMessage = "İçerik en az 10 karakter olmalıdır")]
    [SafeHtml(ErrorMessage = "İçerik güvenli HTML formatında olmalıdır")]
    public string Icerik { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz")]
    public int? SablonId { get; set; }

    /// <summary>
    /// Banner/Logo Dosya ID (Opsiyonel - Email üstünde görünecek görsel)
    /// </summary>
    public int? BannerDosyaId { get; set; }

    /// <summary>
    /// Duyuru imza kategorisi (GENEL_DUYURU_IMZASIZ, REKTOR, BID, vb.) - EMAIL_IMZA tablosundan
    /// </summary>
    [Required(ErrorMessage = "Duyuru kategorisi zorunludur")]
    [StringLength(50, ErrorMessage = "Duyuru kategorisi en fazla 50 karakter olmalıdır")]
    public string DuyuruKategorisi { get; set; } = string.Empty;

    /// <summary>
    /// SMTP gönderici kategorisi (EMAIL_REKTOR, EMAIL_BID, EMAIL_DUYURU, vb.)
    /// Hangi email hesabından gönderileceğini belirler
    /// </summary>
    [Required(ErrorMessage = "Gönderici kategorisi zorunludur")]
    [StringLength(50, ErrorMessage = "Gönderici kategorisi en fazla 50 karakter olmalıdır")]
    public string GondericiKategori { get; set; } = "EMAIL_DUYURU";

    /// <summary>
    /// Duyuruyu onaylayacak kullanıcının ID'si (opsiyonel - MANAGER veya ADMIN rolünde olmalı)
    /// </summary>
    public int? OnaylayanKullaniciId { get; set; }

    /// <summary>
    /// Alıcı grup ID listesi
    /// </summary>
    public List<int> GrupIdList { get; set; } = new();

    /// <summary>
    /// Manuel alıcı email listesi
    /// </summary>
    public List<string> AliciEmailList { get; set; } = new();

    /// <summary>
    /// Duyuruya eklenecek dosya ID listesi
    /// </summary>
    public List<int> DosyaIdList { get; set; } = new();

    /// <summary>
    /// Zamanlanmış gönderim tarihi (opsiyonel)
    /// </summary>
    public DateTime? ZamanlanmisTarih { get; set; }

    /// <summary>
    /// Tekrarlama sıklığı (NONE, DAILY, WEEKLY, MONTHLY)
    /// </summary>
    public string? TekrarSikligi { get; set; }

    /// <summary>
    /// Tekrarlama bitiş tarihi
    /// </summary>
    public DateTime? TekrarBitisTarihi { get; set; }

    /// <summary>
    /// Duyuru açıklaması (opsiyonel)
    /// </summary>
    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olmalıdır")]
    public string? Aciklama { get; set; }
}

public class AnnouncementDetailView
{
    public int Id { get; set; }
    public string Konu { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;

    [JsonPropertyName("durum")]
    public string DuyuruDurumu { get; set; } = string.Empty;

    public DateTime? GonderimTarihi { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? OnayNotu { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public int? SablonId { get; set; }
    public string? SablonAdi { get; set; }
    public int OlusturanKullaniciId { get; set; }
    public string? OlusturanKullaniciAdi { get; set; }
    public int? OnaylayanKullaniciId { get; set; }
    public string? OnaylayanKullaniciAdi { get; set; }
    public string DuyuruKategorisi { get; set; } = "GENEL_DUYURU_IMZASIZ";
    public string GondericiKategori { get; set; } = "EMAIL_DUYURU";
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    public bool DosyaVarMi { get; set; }

    // Red bilgileri (çift aşamalı red için)
    public string? KoordinatorRedNotu { get; set; }

    public DateTime? KoordinatorRedTarihi { get; set; }
    public string? ManagerRedNotu { get; set; }
    public DateTime? ManagerRedTarihi { get; set; }

    // Alıcı bilgileri
    public int ToplamAliciSayisi { get; set; }

    // Alıcı listeleri - düzenleme için
    public List<int> GrupIdList { get; set; } = new();

    public List<string> AliciEmailList { get; set; } = new();
}

public class AnnouncementMovementView
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string? OncekiDurum { get; set; }
    public string YeniDurum { get; set; } = string.Empty;
    public string IslemTipi { get; set; } = string.Empty;
    public int? KullaniciId { get; set; }
    public string? KullaniciAdi { get; set; }
    public string? Aciklama { get; set; }
    public int? SecilenOnaylayiciId { get; set; }
    public string? SecilenOnaylayiciAdi { get; set; }
    public DateTime IslemTarihi { get; set; }
}

public class TemplateDetailView
{
    public int Id { get; set; }
    public string SablonAdi { get; set; } = string.Empty;
    public string? KonuSablonu { get; set; }
    public string IcerikSablonu { get; set; } = string.Empty;
    public int? KategoriId { get; set; }
    public string Varsayilan { get; set; } = string.Empty;
    public string Aktif { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    public int KullanimSayisi { get; set; } // Kaç duyuruda kullanılmış

    // Kategori bilgisi (navigation property olarak)
    public TemplateCategoryInfo? Kategori { get; set; }
}

public class TemplateCategoryInfo
{
    public int Id { get; set; }
    public string KategoriAdi { get; set; } = string.Empty;
    public string Renk { get; set; } = string.Empty;
    public string Ikon { get; set; } = string.Empty;
}

public class FileDetailView
{
    public int Id { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    public int? DuyuruId { get; set; }
    public string? Konu { get; set; }
    public DateTime YuklemeTarihi { get; set; }
}

public class PendingApprovalView
{
    public int Id { get; set; }
    public string Konu { get; set; } = string.Empty;
    public string IcerikTipi { get; set; } = "EMAIL";
    public string Durum { get; set; } = string.Empty;
    public int OlusturanKullaniciId { get; set; }
    public string? OlusturanKullaniciAdi { get; set; }

    // İki aşamalı onay bilgileri
    public int? IlkOnaylayanKullaniciId { get; set; }  // Koordinatör
    public string? IlkOnaylayanKullaniciAdi { get; set; }
    public int? SonOnaylayanKullaniciId { get; set; }  // Manager (atanan)
    public string? SonOnaylayanKullaniciAdi { get; set; }

    // Backward compatibility
    public int? OnaylayanKullaniciId { get; set; }
    public string? OnaylayanKullaniciAdi { get; set; }
    public string? OnayNotu { get; set; }  // Koordinatör notu
    public int ToplamAliciSayisi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
}

public class ApprovedAnnouncementView
{
    public int Id { get; set; }
    public string Konu { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;

    // Oluşturan bilgileri
    public int OlusturanKullaniciId { get; set; }

    public string? OlusturanKullaniciAdi { get; set; }

    // İki aşamalı onay bilgileri
    public int? IlkOnaylayanKullaniciId { get; set; }  // Koordinatör

    public string? IlkOnaylayanKullaniciAdi { get; set; }
    public int? SonOnaylayanKullaniciId { get; set; }  // Manager
    public string? SonOnaylayanKullaniciAdi { get; set; }

    // Backward compatibility (deprecated - kullanılmamalı)
    public int? OnaylayanKullaniciId { get; set; }

    public string? OnaylayanKullaniciAdi { get; set; }

    // İşlem bilgileri (Hareket tablosundan)
    public DateTime? IslemTarihi { get; set; }  // Gerçek onay/red tarihi

    public string? IslemNotu { get; set; }  // Onay/Red notu
    public string? IslemYapan { get; set; }  // İşlem yapan kişi adı

    // Tarih bilgileri
    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? OnayTarihi { get; set; }  // Deprecated - IslemTarihi kullanılmalı
    public DateTime? GuncellemeTarihi { get; set; }
}

/// <summary>
/// Alıcı önizleme ve sayı bilgisi DTO
/// </summary>
public class RecipientPreviewDto
{
    public int TotalRecipientCount { get; set; }
    public int ToCount { get; set; }
    public int CcCount { get; set; }
    public int BccCount { get; set; }
    public int GroupCount { get; set; }
    public int ManualCount { get; set; }

    public List<RecipientPreviewItem> Recipients { get; set; } = new();
    public List<GroupPreviewItem> Groups { get; set; } = new();
}

/// <summary>
/// Alıcı önizleme item
/// </summary>
public class RecipientPreviewItem
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Category { get; set; } = string.Empty; // TO, CC, BCC
    public string Source { get; set; } = string.Empty; // GROUP, MANUAL
    public string? GroupName { get; set; }
}

/// <summary>
/// Grup önizleme item
/// </summary>
public class GroupPreviewItem
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // TO, CC, BCC
    public int MemberCount { get; set; }
    public bool IsBccOnly { get; set; }
}

/// <summary>
/// Duyuru önizleme DTO
/// </summary>
public class AnnouncementPreviewDto
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    public RecipientPreviewDto Recipients { get; set; } = new();

    public List<AttachmentPreviewItem> Attachments { get; set; } = new();

    public string TemplateName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Ek dosya önizleme item
/// </summary>
public class AttachmentPreviewItem
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
}

/// <summary>
/// Email önizleme - AnnouncementPreviewDto için alias
/// </summary>
public class EmailPreviewView : AnnouncementPreviewDto
{
}

/// <summary>
/// Duyuru istatistikleri
/// </summary>
public class AnnouncementStatistics
{
    public int TotalAnnouncements { get; set; }
    public int DraftCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int SentCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalRecipients { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
}

/// <summary>
/// Durum değiştirme isteği - validation dahil
/// </summary>
public class ChangeStatusRequest
{
    [Required(ErrorMessage = "Yeni durum zorunludur")]
    [DuyuruDurumValidation(ErrorMessage = "Geçersiz durum değeri")]
    public string YeniDurum { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Not 500 karakteri geçemez")]
    public string? Note { get; set; }
}

public class SendTestEmailRequest
{
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    public string? TestEmail { get; set; }
}

public class SaveAsTemplateRequest
{
    [Required(ErrorMessage = "Şablon adı zorunludur")]
    [StringLength(100, ErrorMessage = "Şablon adı 100 karakteri geçemez")]
    public string TemplateName { get; set; } = string.Empty;
}