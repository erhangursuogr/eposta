namespace DeuEposta.Models;

/// <summary>
/// Duyuru hareketleri (Audit Trail) - Tüm durum değişiklikleri ve işlemler
/// SQL: EPOSTA_DUYURU_HAREKETLERI
/// </summary>
public class EpostaDuyuruHareket
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string? OncekiDurum { get; set; } // NULL olabilir (ilk oluşturma)
    public string YeniDurum { get; set; } = string.Empty;
    public string IslemTipi { get; set; } = string.Empty; // OLUSTURMA, ONAYA_GONDERME, ONAYLAMA, REDDETME, IPTAL, GONDERIM, DUZENLEME, SILME
    public int? KullaniciId { get; set; } // NULL olabilir (sistem işlemleri için)
    public string? Aciklama { get; set; } // Onay/Red notu
    public int? SecilenOnaylayiciId { get; set; } // Kontrolörün seçtiği manager ID'si (sadece ONAYLAMA işleminde)
    public DateTime IslemTarihi { get; set; } = DateTime.Now;

    // Navigation properties
    public EpostaDuyuru? Duyuru { get; set; }

    public Kullanici? Kullanici { get; set; }
    public Kullanici? SecilenOnaylayici { get; set; }
}