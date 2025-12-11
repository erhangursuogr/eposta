using DeuEposta.Models.Enums;

namespace DeuEposta.Models;

public class EpostaDuyuru
{
    public int Id { get; set; }
    public string Konu { get; set; } = string.Empty; // Email subject + Display title
    public string Icerik { get; set; } = string.Empty; // CLOB - HTML (email) veya Plain Text (sosyal medya)
    public string? Aciklama { get; set; } // Duyuru açıklaması (EDITOR tarafından doldurulur, Kontrolör/manager için bilgi)
    public string IcerikTipi { get; set; } = "EMAIL"; // EMAIL, SOSYAL_MEDYA
    public int? BannerDosyaId { get; set; } // Ana banner/resim dosyası
    public int? SablonId { get; set; }
    public int OlusturanKullaniciId { get; set; }
    public string Durum { get; set; } = DuyuruDurum.TASLAK;
    public string DuyuruKategorisi { get; set; } = "GENEL_DUYURU_IMZASIZ"; // Email imza kategorisi (EMAIL_IMZA tablosundan - GENEL_DUYURU_IMZASIZ, REKTOR, BID, vb.)
    public string GondericiKategori { get; set; } = "EMAIL_DUYURU"; // SMTP gönderici kategorisi (EMAIL_REKTOR, EMAIL_BID, EMAIL_DUYURU, vb.)
    public int? IlkOnaylayanKullaniciId { get; set; } // Koordinatör (ilk onaylayan)
    public int? SonOnaylayanKullaniciId { get; set; } // Manager (son onaylayan)
    public DateTime? GercekGonderimTarihi { get; set; } // Gerçek gönderim tarihi (sadece EMAIL için)
    public int ToplamAliciSayisi { get; set; } = 0; // Sadece EMAIL için
    public int BasariliGonderimSayisi { get; set; } = 0; // Sadece EMAIL için
    public int BasarisizGonderimSayisi { get; set; } = 0; // Sadece EMAIL için
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public EpostaSablon? Sablon { get; set; }

    public Kullanici? OlusturanKullanici { get; set; }
    public Kullanici? IlkOnaylayanKullanici { get; set; } // Koordinatör (ilk onaylayan)
    public Kullanici? SonOnaylayanKullanici { get; set; } // Manager (son onaylayan)
    public Dosya? BannerDosya { get; set; }
    public ICollection<EpostaDuyuruAlici> Alicilar { get; set; } = new List<EpostaDuyuruAlici>(); // Sadece EMAIL için
    public ICollection<Dosya> EkDosyalar { get; set; } = new List<Dosya>(); // Duyuruya bağlı tüm dosyalar
    public ICollection<EpostaDuyuruHareket> Hareketler { get; set; } = new List<EpostaDuyuruHareket>(); // Audit trail

    // Business logic methods
    /// <summary>
    /// Durumun geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool IsValidDurum()
    {
        return DuyuruDurum.IsValidDurum(Durum);
    }

    /// <summary>
    /// Belirtilen duruma geçişin mümkün olup olmadığını kontrol eder
    /// </summary>
    public bool CanTransitionTo(string yeniDurum)
    {
        return DuyuruDurum.IsValidTransition(Durum, yeniDurum);
    }

    /// <summary>
    /// Durum geçişi yapar - validation dahil
    /// </summary>
    public bool TryChangeDurum(string yeniDurum, out string? hata)
    {
        hata = null;

        if (!DuyuruDurum.IsValidDurum(yeniDurum))
        {
            hata = $"Geçersiz durum: {yeniDurum}";
            return false;
        }

        if (!DuyuruDurum.IsValidTransition(Durum, yeniDurum))
        {
            hata = $"'{Durum}' durumundan '{yeniDurum}' durumuna geçiş yapılamaz";
            return false;
        }

        Durum = yeniDurum;
        GuncellemeTarihi = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Bu duyurunun düzenlenebilir olup olmadığını kontrol eder
    /// </summary>
    public bool IsEditable()
    {
        return Durum is DuyuruDurum.TASLAK or DuyuruDurum.REDDEDILDI;
    }

    /// <summary>
    /// Bu duyurunun onay için gönderilebilir olup olmadığını kontrol eder
    /// TASLAK veya REDDEDILDI durumunda onaya gönderilebilir
    /// </summary>
    public bool CanSubmitForApproval()
    {
        // EMAIL ise alıcı kontrolü, SOSYAL_MEDYA ise sadece durum kontrolü
        if (IcerikTipi == "EMAIL")
            return (Durum is DuyuruDurum.TASLAK or DuyuruDurum.REDDEDILDI) && (Alicilar?.Any() ?? false);

        return Durum is DuyuruDurum.TASLAK or DuyuruDurum.REDDEDILDI;
    }

    /// <summary>
    /// Bu duyurunun gönderilebilir olup olmadığını kontrol eder (sadece EMAIL için)
    /// </summary>
    public bool CanSend()
    {
        return IcerikTipi == "EMAIL" && Durum is DuyuruDurum.ONAYLANDI && (Alicilar?.Any() ?? false);
    }

    /// <summary>
    /// Bu duyurunun EMAIL tipinde olup olmadığını kontrol eder
    /// </summary>
    public bool IsEmailType()
    {
        return IcerikTipi == "EMAIL";
    }

    /// <summary>
    /// Bu duyurunun SOSYAL_MEDYA tipinde olup olmadığını kontrol eder
    /// </summary>
    public bool IsSocialMediaType()
    {
        return IcerikTipi == "SOSYAL_MEDYA";
    }
}