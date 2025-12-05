using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeuEposta.Models;

/// <summary>
/// Duyuru zamanlamaları - Her duyuru için birden fazla zamanlama oluşturulabilir
/// Örn: 1 ay boyunca her 5 günde bir tekrar gönderme gibi
/// </summary>
[Table("EPOSTA_DUYURU_ZAMANLAMALAR")]
public class EpostaDuyuruZamanlama
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("DUYURU_ID")]
    public int DuyuruId { get; set; }

    /// <summary>
    /// Zamanlanmış gönderim tarihi ve saati
    /// </summary>
    [Column("ZAMANLANAN_TARIH")]
    public DateTime ZamanlanmaTarihi { get; set; }

    /// <summary>
    /// Zamanlama durumu: BEKLEMEDE, GONDERILDI, IPTAL, HATA
    /// </summary>
    [Column("DURUM")]
    [MaxLength(50)]
    public string Durum { get; set; } = "BEKLEMEDE";

    /// <summary>
    /// Gerçek gönderim tarihi (zamanlaması geldiğinde)
    /// </summary>
    [Column("GONDERIM_TARIHI")]
    public DateTime? GonderimTarihi { get; set; }

    /// <summary>
    /// Hangfire job ID - iptal işlemi için kullanılır
    /// </summary>
    [Column("HANGFIRE_JOB_ID")]
    [MaxLength(200)]
    public string? HangfireJobId { get; set; }

    /// <summary>
    /// Hata mesajı (gönderim başarısız olursa)
    /// </summary>
    [Column("HATA_MESAJI")]
    [MaxLength(1000)]
    public string? HataMesaji { get; set; }

    /// <summary>
    /// Kaç alıcıya gönderildi
    /// </summary>
    [Column("ALICI_SAYISI")]
    public int AliciSayisi { get; set; } = 0;

    /// <summary>
    /// Zamanlamayı oluşturan kullanıcı
    /// </summary>
    [Column("OLUSTURAN_KULLANICI_ID")]
    public int OlusturanKullaniciId { get; set; }

    [Column("OLUSTURMA_TARIHI")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    [Column("GUNCELLEME_TARIHI")]
    public DateTime? GuncellemeTarihi { get; set; }

    /// <summary>
    /// İptal notu (kullanıcı zamanlamayı iptal ederse)
    /// </summary>
    [Column("IPTAL_NOTU")]
    [MaxLength(500)]
    public string? IptalNotu { get; set; }

    // Navigation properties
    public EpostaDuyuru? Duyuru { get; set; }

    public Kullanici? OlusturanKullanici { get; set; }
}

/// <summary>
/// Zamanlama durumları
/// </summary>
public static class ZamanlamaDurum
{
    public const string BEKLEMEDE = "BEKLEMEDE";
    public const string GONDERILDI = "GONDERILDI";
    public const string IPTAL = "IPTAL";
    public const string HATA = "HATA";

    public static bool IsValidDurum(string durum)
    {
        return durum == BEKLEMEDE || durum == GONDERILDI || durum == IPTAL || durum == HATA;
    }
}