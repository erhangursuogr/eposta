namespace DeuEposta.Models.DTOs;

/// <summary>
/// Login log kayıtları için DTO
/// </summary>
public class LoginLogDto
{
    public int Id { get; set; }
    public int? KullaniciId { get; set; }
    public string? KullaniciAdi { get; set; }
    public string? Email { get; set; }
    public string? IpAdres { get; set; }
    public string? UserAgent { get; set; }
    public string GirisTuru { get; set; } = string.Empty; // LDAP, LOCAL, API
    public bool Basarili { get; set; }
    public string? HataMesaji { get; set; }
    public DateTime GirisTarihi { get; set; }
}

/// <summary>
/// Sistem log kayıtları için DTO
/// </summary>
public class SystemLogDto
{
    public int Id { get; set; }
    public int? KullaniciId { get; set; }
    public string? KullaniciAdi { get; set; }
    public string LogSeviye { get; set; } = string.Empty; // ERROR, WARN, INFO, DEBUG
    public string Kategori { get; set; } = string.Empty; // EMAIL, USER, GROUP, FILE, SYSTEM
    public string Islem { get; set; } = string.Empty;
    public string? Detay { get; set; }
    public string? IpAdres { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LogTarihi { get; set; }
}

/// <summary>
/// Email gönderim log kayıtları için DTO
/// </summary>
public class EmailLogDto
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string? DuyuruKonu { get; set; }
    public string AliciEmail { get; set; } = string.Empty;
    public string? AliciAdSoyad { get; set; }
    public string AliciKategorisi { get; set; } = "TO"; // TO, CC, BCC
    public bool GonderimBasarili { get; set; }
    public string? HataMesaji { get; set; }
    public DateTime GonderimTarihi { get; set; }
}

/// <summary>
/// Log filtreleme için request model
/// </summary>
public class LogFilterRequest
{
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string? Arama { get; set; } // Genel arama (email, kullanıcı adı, ip, vb.)
    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 50;
}

/// <summary>
/// Login log filtreleme için özel request
/// </summary>
public class LoginLogFilterRequest : LogFilterRequest
{
    public bool? SadeceBasarisiz { get; set; }
    public string? GirisTuru { get; set; } // LDAP, LOCAL, API
}

/// <summary>
/// Sistem log filtreleme için özel request
/// </summary>
public class SystemLogFilterRequest : LogFilterRequest
{
    public string? LogSeviye { get; set; } // ERROR, WARN, INFO, DEBUG
    public string? Kategori { get; set; } // EMAIL, USER, GROUP, FILE, SYSTEM
    public bool? SadeceHata { get; set; }
}

/// <summary>
/// Email log filtreleme için özel request
/// </summary>
public class EmailLogFilterRequest : LogFilterRequest
{
    public int? DuyuruId { get; set; }
    public bool? SadeceBasarisiz { get; set; }
}

/// <summary>
/// Sayfalandırılmış log response
/// </summary>
public class PagedLogResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int ToplamKayit { get; set; }
    public int Sayfa { get; set; }
    public int SayfaBoyutu { get; set; }
    public int ToplamSayfa => (int)Math.Ceiling(ToplamKayit / (double)SayfaBoyutu);
}