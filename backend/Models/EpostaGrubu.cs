namespace DeuEposta.Models;

public class EpostaGrubu
{
    public int Id { get; set; }
    public string GrupAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public string GrupTipi { get; set; } = string.Empty; // MANUEL, DOSYA, DINAMIK, DEBIS
    public string? ViewAdi { get; set; } // DINAMIK/DOSYA gruplar için view/dosya adı
    public string? FilterKosulu { get; set; } // DINAMIK gruplar için WHERE koşulu
    public string? ListeciEmail { get; set; } // DEBIS gruplar için listeci email adresi
    public int UyeSayisi { get; set; } // Grup üye sayısı (DOSYA: trigger günceller, DINAMIK: backend hesaplar)

    public string Aktif { get; set; } = "Y";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation properties
    public ICollection<EpostaGrupUyesi> Uyeler { get; set; } = new List<EpostaGrupUyesi>();

    public ICollection<EpostaDuyuruAlici> DuyuruAlicilari { get; set; } = new List<EpostaDuyuruAlici>();
}