namespace DeuEposta.Models;

public class LogSistem
{
    public int Id { get; set; }
    public int? KullaniciId { get; set; }
    public string LogSeviye { get; set; } = string.Empty; // ERROR, WARN, INFO, DEBUG
    public string Kategori { get; set; } = string.Empty; // EMAIL, USER, GROUP, FILE, SYSTEM
    public string Islem { get; set; } = string.Empty;
    public string? Detay { get; set; } // CLOB
    public string? IpAdres { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LogTarihi { get; set; } = DateTime.Now;

    // Navigation properties
    public Kullanici? Kullanici { get; set; }
}