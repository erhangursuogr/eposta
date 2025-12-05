namespace DeuEposta.Models;

public class DashboardOzetView
{
    // Database view column names - V_DASHBOARD_OZET
    public int TaslakSayisi { get; set; }           // TASLAK_SAYISI

    public int OnayBekliyor { get; set; }           // ONAY_BEKLIYOR_SAYISI
    public int OnaylandiSayisi { get; set; }        // ONAYLANDI_SAYISI
    public int GonderildiSayisi { get; set; }       // GONDERILDI_SAYISI
    public int ToplamDuyuru { get; set; }           // TOPLAM_DUYURU
    public int AktifKullaniciSayisi { get; set; }   // AKTIF_KULLANICI_SAYISI
    public int AdminSayisi { get; set; }            // ADMIN_SAYISI
    public int AktifGrupSayisi { get; set; }        // AKTIF_GRUP_SAYISI
    public int DinamikGrupSayisi { get; set; }      // DINAMIK_GRUP_SAYISI
    public int Son30GunAlici { get; set; }          // SON_30GUN_ALICI
    public int Son30GunBasarili { get; set; }       // SON_30GUN_BASARILI
    public int HesaplanmasiGerekenler { get; set; } // HESAPLANMASI_GEREKENLER
    public DateTime RaporTarihi { get; set; }       // RAPOR_TARIHI
}