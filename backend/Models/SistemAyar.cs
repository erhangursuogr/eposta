namespace DeuEposta.Models;

public class SistemAyar
{
    public int Id { get; set; }
    public string AyarKategori { get; set; } = string.Empty;
    public string AyarAnahtar { get; set; } = string.Empty;
    public string? AyarDeger { get; set; }
    public string? AyarAciklama { get; set; }
    public int? GorevYeri { get; set; } // İmza için görev yeri kısıtlaması (NULL = tüm görev yerleri)
    public string Gizli { get; set; } = "N";
    public string Aktif { get; set; } = "Y";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }
}