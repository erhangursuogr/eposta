namespace DeuEposta.Models;

public class LogLogin
{
    public int Id { get; set; }
    public int? KullaniciId { get; set; }
    public string? KullaniciAdi { get; set; }
    public string? Email { get; set; }
    public string? IpAdres { get; set; }
    public string? UserAgent { get; set; }
    public string GirisTuru { get; set; } = string.Empty; // LDAP, LOCAL, API
    public string Basarili { get; set; } = "Y"; // Y, N
    public string? HataMesaji { get; set; }
    public DateTime GirisTarihi { get; set; } = DateTime.Now;

    // Navigation properties
    public Kullanici? Kullanici { get; set; }
}