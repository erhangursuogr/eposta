namespace DeuEposta.Models;

public class EpostaDuyuruGonderimLog
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string AliciEmail { get; set; } = string.Empty;
    public string? AliciAdSoyad { get; set; }
    public string AliciKategorisi { get; set; } = "TO"; // TO, CC, BCC
    public string GonderimDurumu { get; set; } = "BASARILI"; // BASARILI, BASARISIZ
    public string? HataMesaji { get; set; }
    public DateTime GonderimTarihi { get; set; } = DateTime.Now;
    public int? AliciId { get; set; } // EpostaDuyuruAlici tablosundaki ID (opsiyonel)

    // Navigation properties
    public EpostaDuyuru? Duyuru { get; set; }

    public EpostaDuyuruAlici? Alici { get; set; }
}