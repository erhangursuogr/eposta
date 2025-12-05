namespace DeuEposta.Models;

public class EpostaDuyuruAlici
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string AliciTipi { get; set; } = "GRUP"; // GRUP, MANUEL
    public string AliciKategorisi { get; set; } = "BCC"; // TO, CC, BCC
    public int? GrupId { get; set; }
    public string? Email { get; set; }
    public string? AdSoyad { get; set; }
    public string GonderimDurumu { get; set; } = "BEKLIYOR"; // BEKLIYOR, GONDERILDI, BASARISIZ, IPTAL
    public DateTime? GonderimTarihi { get; set; }
    public string? HataMesaji { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    // Navigation properties
    public EpostaDuyuru? Duyuru { get; set; }

    public EpostaGrubu? Grup { get; set; }
}