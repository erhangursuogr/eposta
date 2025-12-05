namespace DeuEposta.Models;

public class EpostaGrupUyesi
{
    public int Id { get; set; }
    public int GrupId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AdSoyad { get; set; }
    public string? Departman { get; set; }
    public string Durum { get; set; } = "AKTIF";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public EpostaGrubu? Grup { get; set; }
}