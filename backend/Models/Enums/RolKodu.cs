namespace DeuEposta.Models.Enums;

/// <summary>
/// Rol kodları - Database ROLLER tablosu ile uyumlu
/// İki Aşamalı Onay Workflow:
/// ADMIN: Tüm yetkiler (bypass onay)
/// MANAGER: Son onay seviyesi (SON_ONAY_BEKLIYOR -> ONAYLANDI)
/// COORDINATOR: İlk onay seviyesi + Manager seçimi (ILK_ONAY_BEKLIYOR -> SON_ONAY_BEKLIYOR)
/// EDITOR: Duyuru oluşturma ve düzenleme
/// VIEWER: Sadece görüntüleme
/// </summary>
public static class RolKodu
{
    public const string ADMIN = "ADMIN";
    public const string MANAGER = "MANAGER";
    public const string COORDINATOR = "COORDINATOR";
    public const string EDITOR = "EDITOR";
    public const string VIEWER = "VIEWER";

    /// <summary>
    /// Tüm geçerli rol kodları
    /// </summary>
    public static readonly string[] TumRoller =
    {
        ADMIN,
        MANAGER,
        COORDINATOR,
        EDITOR,
        VIEWER
    };

    /// <summary>
    /// Kontrolör onayı yapabilecek roller
    /// </summary>
    public static readonly string[] IlkOnayYetkileri = { ADMIN, COORDINATOR };

    /// <summary>
    /// Manager (son) onayı yapabilecek roller
    /// </summary>
    public static readonly string[] SonOnayYetkileri = { ADMIN, MANAGER };

    /// <summary>
    /// Direkt gönderim yapabilecek roller (onaysız)
    /// </summary>
    public static readonly string[] DirektGonderimYetkileri = { ADMIN };

    /// <summary>
    /// Duyuru oluşturabilecek roller
    /// </summary>
    public static readonly string[] DuyuruOlusturmaYetkileri = { ADMIN, MANAGER, COORDINATOR, EDITOR };

    /// <summary>
    /// Kullanıcı yönetimi yapabilecek roller
    /// </summary>
    public static readonly string[] KullaniciYonetimiYetkileri = { ADMIN };

    /// <summary>
    /// Sistem ayarlarını değiştirebilecek roller
    /// </summary>
    public static readonly string[] SistemAyarlariYetkileri = { ADMIN };

    /// <summary>
    /// Belirtilen rolün ilk onay yetkisi olup olmadığını kontrol eder
    /// </summary>
    public static bool CanFirstApprove(string? rolKodu)
    {
        return !string.IsNullOrEmpty(rolKodu) && IlkOnayYetkileri.Contains(rolKodu);
    }

    /// <summary>
    /// Belirtilen rolün son onay yetkisi olup olmadığını kontrol eder
    /// </summary>
    public static bool CanFinalApprove(string? rolKodu)
    {
        return !string.IsNullOrEmpty(rolKodu) && SonOnayYetkileri.Contains(rolKodu);
    }

    /// <summary>
    /// Belirtilen rolün direkt gönderim yetkisi olup olmadığını kontrol eder
    /// </summary>
    public static bool CanDirectSend(string? rolKodu)
    {
        return !string.IsNullOrEmpty(rolKodu) && DirektGonderimYetkileri.Contains(rolKodu);
    }

    /// <summary>
    /// Belirtilen rolün duyuru oluşturma yetkisi olup olmadığını kontrol eder
    /// </summary>
    public static bool CanCreateAnnouncement(string? rolKodu)
    {
        return !string.IsNullOrEmpty(rolKodu) && DuyuruOlusturmaYetkileri.Contains(rolKodu);
    }

    /// <summary>
    /// Belirtilen rolün geçerli olup olmadığını kontrol eder
    /// </summary>
    public static bool IsValidRol(string? rolKodu)
    {
        return !string.IsNullOrEmpty(rolKodu) && TumRoller.Contains(rolKodu);
    }

    /// <summary>
    /// Rolün kullanıcı dostu açıklamasını getirir
    /// </summary>
    public static string GetRolAciklama(string rolKodu)
    {
        return rolKodu switch
        {
            ADMIN => "Sistem Yöneticisi - Tüm yetkiler",
            MANAGER => "Üst Onaylayıcı - Son onay seviyesi",
            COORDINATOR => "Kontrolör - İlk onay seviyesi",
            EDITOR => "Editör - Duyuru oluşturma ve düzenleme",
            VIEWER => "Görüntüleyici - Sadece okuma yetkisi",
            _ => "Bilinmeyen Rol"
        };
    }
}