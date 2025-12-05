namespace DeuEposta.Models;

public class EpostaSablon
{
    public int Id { get; set; }
    public string SablonAdi { get; set; } = string.Empty;
    public string? KonuSablonu { get; set; } = string.Empty; // DB: KONU_SABLONU
    public string IcerikSablonu { get; set; } = string.Empty; // DB: ICERIK_SABLONU (CLOB)
    public int? KategoriId { get; set; } // DB: KATEGORI_ID
    public string Varsayilan { get; set; } = "N"; // DB: VARSAYILAN
    public string Aktif { get; set; } = "Y";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public EpostaSablonKategori? Kategori { get; set; }

    public ICollection<EpostaDuyuru> Duyurular { get; set; } = new List<EpostaDuyuru>();
}