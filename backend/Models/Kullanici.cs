namespace DeuEposta.Models;

public class Kullanici
{
    public int Id { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public int? GorevYeri { get; set; } // Görev yeri kodu (0=Rektörlük, 500=Mühendislik vb.) - Oracle 11g'den otomatik çekilir
    public string? GorevYeriAdi { get; set; } // Görev yeri adı - Oracle 11g'den otomatik çekilir
    public int RolId { get; set; }

    // public string? ParolaHash { get; set; } // REMOVED - LDAP only authentication
    public string Aktif { get; set; } = "Y";

    public DateTime? SonGirisTarihi { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public Rol? Rol { get; set; }

    public ICollection<EpostaDuyuru> OlusturulanDuyurular { get; set; } = new List<EpostaDuyuru>();
    public ICollection<EpostaDuyuru> IlkOnaylananDuyurular { get; set; } = new List<EpostaDuyuru>(); // Koordinatör olarak onayladığı duyurular
    public ICollection<EpostaDuyuru> SonOnaylananDuyurular { get; set; } = new List<EpostaDuyuru>(); // Manager olarak onayladığı duyurular
    public ICollection<LogLogin> LoginLoglari { get; set; } = new List<LogLogin>();
    public ICollection<LogSistem> SistemLoglari { get; set; } = new List<LogSistem>();
}