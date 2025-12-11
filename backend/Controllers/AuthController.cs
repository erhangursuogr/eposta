using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISecurityService _securityService;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ISecurityService securityService,
        ISystemSettingsService systemSettingsService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _securityService = securityService;
        _systemSettingsService = systemSettingsService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var clientIP = HttpContextHelper.GetClientIPAddress(HttpContext);
        var userAgent = Request.Headers["User-Agent"].ToString();

        var response = await _authService.LoginAsync(request, clientIP, userAgent);

        // SECURITY: Token'ı HttpOnly cookie'de gönder (XSS koruması)
        if (response.Success && response.Data?.Token != null)
        {
            var jwtExpiration = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "720");
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,        // JavaScript'ten erişilemez (XSS koruması)
                Secure = Request.IsHttps, // HTTPS'te true, HTTP'de false (development için)
                SameSite = SameSiteMode.Lax, // Strict yerine Lax (CORS ile uyumlu)
                Expires = DateTimeOffset.UtcNow.AddMinutes(jwtExpiration), // Config'den (default: 720 dakika)
                Path = "/"
            };

            Response.Cookies.Append("auth_token", response.Data.Token, cookieOptions);
            _logger.LogInformation("Token set in HttpOnly cookie for user {Email} (Secure={Secure})", request.Email, cookieOptions.Secure);

            // NOTE: Token response'da da kalıyor (Postman/development test için)
            // Frontend cookie kullanacak, Postman Authorization header kullanabilir
            // Production'da frontend cookie'den okur, token response'unda olması zararsız
        }

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            429 => StatusCode(429, response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var jti = User.FindFirst("jti")?.Value;
        var ipAddress = HttpContextHelper.GetClientIPAddress(HttpContext);

        var response = await _authService.LogoutAsync(email, jti, ipAddress);

        // SECURITY: Cookie'leri sil (auth_token + id_token)
        if (response.Success)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };

            Response.Cookies.Delete("auth_token", cookieOptions);
            Response.Cookies.Delete("id_token", cookieOptions); // SSO id_token
            _logger.LogInformation("Auth and id_token cookies deleted for user {Email}", email);
        }

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        var response = await _authService.GetCurrentUserAsync(email);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // ========== SSO KEYCLOAK ENDPOINTS (SIMPLE) ==========

    /// <summary>
    /// AUTH_MODE değerini döndürür (0=LDAP, 1=SSO)
    /// </summary>
    [HttpGet("mode")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAuthMode()
    {
        var mode = await _systemSettingsService.GetSettingValueAsync("AUTH", "MODE", "0");
        return Ok(new { mode });
    }

    /// <summary>
    /// Keycloak SSO callback - Authorization code'u JWT token'a çevirir
    /// </summary>
    [HttpPost("sso/callback")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> SsoCallback([FromBody] SsoCallbackRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return BadRequest(ResponseDataModel<object>.ErrorResult("Authorization code is required", 400));
        }

        var clientIP = HttpContextHelper.GetClientIPAddress(HttpContext);
        var response = await _authService.SsoCallbackAsync(request.Code, clientIP);

        if (response.Success && response.Data != null)
        {
            // HttpOnly cookie set et (auth_token + id_token)
            var jwtExpiration = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "720");
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(jwtExpiration), // Config'den (default: 720 dakika)
                Path = "/"
            };

            Response.Cookies.Append("auth_token", response.Data.LoginData.Token, cookieOptions);

            // SSO id_token cookie (logout için)
            if (!string.IsNullOrEmpty(response.Data.IdToken))
            {
                Response.Cookies.Append("id_token", response.Data.IdToken, cookieOptions);
            }
        }

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

}