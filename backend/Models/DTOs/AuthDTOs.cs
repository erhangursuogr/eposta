using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Models.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Email alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(254, ErrorMessage = "Email adresi 254 karakterden fazla olamaz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre alanı zorunludur")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Şifre 6-128 karakter arasında olmalıdır")]
    public string Password { get; set; } = string.Empty;
}

public class LoginData
{
    public string Token { get; set; } = string.Empty;
    public string? IdToken { get; set; } // SSO Keycloak id_token (logout için)
    public UserInfo User { get; set; } = new();
    public DateTime ExpiresAt { get; set; } // Token expiration timestamp (session timeout warning için)
}

public class UserInfo
{
    public int Id { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public int? GorevYeri { get; set; } // Görev yeri kodu (0=Rektörlük, 500=Mühendislik vb.)
    public string? GorevYeriAdi { get; set; } // Görev yeri adı
    public string Rol { get; set; } = string.Empty;
}

// SSO Keycloak DTOs
public class SsoCallbackRequest
{
    public string Code { get; set; } = string.Empty;
}

public class SsoCallbackResult
{
    public LoginData LoginData { get; set; } = new();
    public string? IdToken { get; set; } // Logout için gerekli
}

public class KeycloakTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? IdToken { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}