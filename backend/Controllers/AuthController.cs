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
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ISecurityService securityService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _securityService = securityService;
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
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,        // JavaScript'ten erişilemez (XSS koruması)
                Secure = Request.IsHttps, // HTTPS'te true, HTTP'de false (development için)
                SameSite = SameSiteMode.Lax, // Strict yerine Lax (CORS ile uyumlu)
                Expires = DateTimeOffset.UtcNow.AddHours(12), // 12 saat
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

        // SECURITY: Cookie'yi sil
        if (response.Success)
        {
            Response.Cookies.Delete("auth_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
            _logger.LogInformation("Auth cookie deleted for user {Email}", email);
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

}