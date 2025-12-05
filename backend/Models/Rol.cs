namespace DeuEposta.Models;

public class Rol
{
    public int Id { get; set; }
    public string RolKodu { get; set; } = string.Empty;
    public string RolAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public int YetkiSeviyesi { get; set; }
    public string Aktif { get; set; } = "Y";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
}