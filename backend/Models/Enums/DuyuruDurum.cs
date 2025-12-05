namespace DeuEposta.Models.Enums;

/// <summary>
/// Duyuru durum değerleri - Database constraint ile uyumlu
/// Çift Onay Workflow: TASLAK -> ILK_ONAY_BEKLIYOR (Kontrolör) -> SON_ONAY_BEKLIYOR (Manager) -> ONAYLANDI -> GONDERILDI
/// Kontrolör ilk onayı yapar ve manager seçer, manager final onayı verir
/// </summary>
public static class DuyuruDurum
{
    public const string TASLAK = "TASLAK";
    public const string ILK_ONAY_BEKLIYOR = "ILK_ONAY_BEKLIYOR"; // Kontrolör onayı
    public const string SON_ONAY_BEKLIYOR = "SON_ONAY_BEKLIYOR"; // Manager onayı
    public const string REDDEDILDI = "REDDEDILDI";
    public const string ONAYLANDI = "ONAYLANDI";
    public const string GONDERILIYOR = "GONDERILIYOR"; // Email gönderiliyor (Hangfire job active)
    public const string GONDERILDI = "GONDERILDI";
    public const string IPTAL = "IPTAL";

    /// <summary>
    /// Tüm geçerli durum değerleri
    /// </summary>
    public static readonly string[] TumDurumlar =
    {
        TASLAK,
        ILK_ONAY_BEKLIYOR,
        SON_ONAY_BEKLIYOR,
        REDDEDILDI,
        ONAYLANDI,
        GONDERILIYOR,
        GONDERILDI,
        IPTAL
    };

    /// <summary>
    /// Durum geçiş kuralları - hangi durumdan hangi duruma geçilebileceğini belirler
    /// Çift onay sistemi: Editor -> Kontrolör -> Manager
    /// </summary>
    public static readonly Dictionary<string, string[]> GecerliGecisler = new()
    {
        [TASLAK] = new[] { ILK_ONAY_BEKLIYOR, IPTAL },
        [ILK_ONAY_BEKLIYOR] = new[] { SON_ONAY_BEKLIYOR, REDDEDILDI, IPTAL }, // Kontrolör onaylar veya reddeder
        [SON_ONAY_BEKLIYOR] = new[] { ONAYLANDI, REDDEDILDI, IPTAL }, // Manager onaylar veya reddeder
        [REDDEDILDI] = new[] { ILK_ONAY_BEKLIYOR, IPTAL }, // Düzenlendikten sonra direkt onaya gönderilebilir
        [ONAYLANDI] = new[] { GONDERILIYOR, IPTAL }, // Job başlar veya iptal edilir
        [GONDERILIYOR] = new[] { GONDERILDI }, // Job tamamlanır
        [GONDERILDI] = new string[] { }, // Kopyalama kullanılır
        [IPTAL] = new[] { ONAYLANDI } // Sadece ADMIN yeniden aktif edebilir (sistemsel hatalar için)
    };

    /// <summary>
    /// Belirtilen durumun geçerli olup olmadığını kontrol eder
    /// </summary>
    public static bool IsValidDurum(string? durum)
    {
        return !string.IsNullOrEmpty(durum) && TumDurumlar.Contains(durum);
    }

    /// <summary>
    /// Durum geçişinin geçerli olup olmadığını kontrol eder
    /// </summary>
    public static bool IsValidTransition(string mevcutDurum, string yeniDurum)
    {
        if (!IsValidDurum(mevcutDurum) || !IsValidDurum(yeniDurum))
            return false;

        return GecerliGecisler.ContainsKey(mevcutDurum) &&
               GecerliGecisler[mevcutDurum].Contains(yeniDurum);
    }

    /// <summary>
    /// Belirtilen durumdan yapılabilecek geçişleri getirir
    /// </summary>
    public static string[] GetAllowedTransitions(string mevcutDurum)
    {
        if (!IsValidDurum(mevcutDurum))
            return Array.Empty<string>();

        return GecerliGecisler.ContainsKey(mevcutDurum)
            ? GecerliGecisler[mevcutDurum]
            : Array.Empty<string>();
    }

    /// <summary>
    /// Durumun kullanıcı dostu açıklamasını getirir
    /// </summary>
    public static string GetDurumAciklama(string durum)
    {
        return durum switch
        {
            TASLAK => "Taslak - Düzenleme aşamasında",
            ILK_ONAY_BEKLIYOR => "İlk Onay Bekliyor - Kontrolör onayında",
            SON_ONAY_BEKLIYOR => "Son Onay Bekliyor - Manager onayında",
            REDDEDILDI => "Reddedildi - Düzenlenmeli ve tekrar onaya gönderilmeli",
            ONAYLANDI => "Onaylandı - Gönderime hazır veya zamanlanabilir",
            GONDERILIYOR => "Gönderiliyor - Email gönderimi devam ediyor",
            GONDERILDI => "Gönderildi - Başarıyla tamamlandı",
            IPTAL => "İptal Edildi - İşlem sonlandırıldı",
            _ => "Bilinmeyen Durum"
        };
    }
}