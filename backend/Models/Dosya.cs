using System.ComponentModel.DataAnnotations.Schema;

namespace DeuEposta.Models;

public class Dosya
{
    public int Id { get; set; }
    public int? DuyuruId { get; set; }
    public int YukleyenKullaniciId { get; set; } // DB: YUKLEYEN_KULLANICI_ID
    public string DosyaAdi { get; set; } = string.Empty; // DB: DOSYA_ADI
    public string DosyaYolu { get; set; } = string.Empty; // DB: DOSYA_YOLU
    public string DosyaTipi { get; set; } = string.Empty; // DB: DOSYA_TIPI (MIME type)
    public string DosyaKategorisi { get; set; } = "ATTACHMENT"; // DB: DOSYA_KATEGORISI
    public long DosyaBoyutu { get; set; } // DB: DOSYA_BOYUTU
    public string? DosyaHash { get; set; } // DB: DOSYA_HASH
    public string? Aciklama { get; set; } // DB: ACIKLAMA

    [Column("SESSION_ID")]
    public string? SessionId { get; set; } // DB: SESSION_ID - Temporary upload session

    public string Aktif { get; set; } = "Y";
    public DateTime YuklemeTarihi { get; set; } = DateTime.Now; // DB: YUKLEME_TARIHI
    public DateTime? GuncellemeTarihi { get; set; }

    // Extra fields not in database (for backwards compatibility)

    [NotMapped]
    public int? IndirmeSayisi { get; set; }

    [NotMapped]
    public DateTime? SonIndirmeTarihi { get; set; }

    // Navigation properties
    public EpostaDuyuru? Duyuru { get; set; }

    public Kullanici? YukleyenKullanici { get; set; }

    // Computed properties
    public bool IsImage => DosyaTipi.StartsWith("image/");

    public bool IsPdf => DosyaTipi == "application/pdf";
    public bool IsDocument => DosyaTipi.Contains("word") || DosyaTipi.Contains("document") || IsPdf;
    public bool IsArchive => DosyaTipi.Contains("zip") || DosyaTipi.Contains("rar");

    public string GetSizeDisplayText()
    {
        if (DosyaBoyutu < 1024)
            return $"{DosyaBoyutu} B";
        else if (DosyaBoyutu < 1024 * 1024)
            return $"{DosyaBoyutu / 1024:F1} KB";
        else if (DosyaBoyutu < 1024 * 1024 * 1024)
            return $"{DosyaBoyutu / (1024 * 1024):F1} MB";
        else
            return $"{DosyaBoyutu / (1024 * 1024 * 1024):F1} GB";
    }
}