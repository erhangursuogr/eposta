namespace DeuEposta.Models;

public class EpostaSablonKategori
{
    public int Id { get; set; }
    public string KategoriAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public string Renk { get; set; } = "#1976d2"; // Hex color code
    public string Ikon { get; set; } = "label"; // Material icon name
    public int SiraNo { get; set; } = 0;
    public string Aktif { get; set; } = "Y";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public ICollection<EpostaSablon> Sablonlar { get; set; } = new List<EpostaSablon>();
}