using System.Text.Json.Serialization;

namespace DeuEposta.Models;

public class DuyuruListeView
{
    // Temel bilgiler
    public int Id { get; set; }

    [JsonPropertyName("konu")]
    public string Konu { get; set; } = string.Empty;

    [JsonPropertyName("durum")]
    public string Durum { get; set; } = string.Empty;

    // Oluşturan kullanıcı
    [JsonPropertyName("olusturanKullaniciId")]
    public int OlusturanKullaniciId { get; set; }

    [JsonPropertyName("olusturanKullaniciAdi")]
    public string? OlusturanAdSoyad { get; set; }

    // Onaylayan kullanıcı (detay için - backward compatibility)
    [JsonPropertyName("onaylayanKullaniciId")]
    public int? OnaylayanKullaniciId { get; set; }

    [JsonPropertyName("onaylayanKullaniciAdi")]
    public string? OnaylayanAdSoyad { get; set; }

    [JsonPropertyName("onayTarihi")]
    public DateTime? OnayTarihi { get; set; }

    [JsonPropertyName("onayNotu")]
    public string? OnayNotu { get; set; }

    // İki aşamalı onay bilgileri
    [JsonPropertyName("ilkOnaylayanKullaniciId")]
    public int? IlkOnaylayanKullaniciId { get; set; }

    [JsonPropertyName("ilkOnaylayanKullaniciAdi")]
    public string? IlkOnaylayanAdSoyad { get; set; }

    [JsonPropertyName("sonOnaylayanKullaniciId")]
    public int? SonOnaylayanKullaniciId { get; set; }

    [JsonPropertyName("sonOnaylayanKullaniciAdi")]
    public string? SonOnaylayanAdSoyad { get; set; }

    // Reddetme bilgileri
    [JsonPropertyName("redNedeni")]
    public string? RedNedeni { get; set; }

    [JsonPropertyName("redTarihi")]
    public DateTime? RedTarihi { get; set; }

    // Alıcı istatistikleri
    [JsonPropertyName("toplamAliciSayisi")]
    public int ToplamAliciSayisi { get; set; }

    [JsonPropertyName("basariliGonderimSayisi")]
    public int BasariliGonderimSayisi { get; set; }

    [JsonPropertyName("basarisizGonderimSayisi")]
    public int BasarisizGonderimSayisi { get; set; }

    // Tarih bilgileri
    [JsonPropertyName("olusturmaTarihi")]
    public DateTime OlusturmaTarihi { get; set; }

    [JsonPropertyName("guncellemeTarihi")]
    public DateTime? GuncellemeTarihi { get; set; }

    [JsonPropertyName("gonderimTarihi")]
    public DateTime? GercekGonderimTarihi { get; set; }

    // Zamanlama bilgisi
    [JsonPropertyName("zamanlanmisMi")]
    public bool ZamanlanmisMi { get; set; }

    [JsonPropertyName("zamanlamaSayisi")]
    public int ZamanlamaSayisi { get; set; }

    // Hesaplanan alanlar
    [JsonPropertyName("basariYuzdesi")]
    public decimal BasariYuzdesi { get; set; }

    [JsonPropertyName("dosyaSayisi")]
    public int ToplamDosyaSayisi { get; set; }
}