using System.Security.Claims;

namespace DeuEposta.Utils;

/// <summary>
/// HttpContext ile ilgili yardımcı metotlar
/// </summary>
public static class HttpContextHelper
{
    /// <summary>
    /// Gerçek client IP adresini al (proxy/load balancer desteği ile)
    /// X-Forwarded-For ve X-Real-IP headerlarını kontrol eder
    /// </summary>
    /// <param name="context">HttpContext instance</param>
    /// <returns>Client IP adresi (örn: "192.168.1.100") veya "Unknown"</returns>
    public static string GetClientIPAddress(HttpContext context)
    {
        if (context == null)
            return "Unknown";

        // 1. X-Forwarded-For header'ından IP al (proxy/load balancer için)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For: client, proxy1, proxy2 formatında olabilir
            // İlk IP gerçek client IP'sidir
            return forwardedFor.Split(',')[0].Trim();
        }

        // 2. X-Real-IP header'ından IP al (bazı proxy'ler bu header'ı kullanır)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 3. Remote IP address (doğrudan bağlantı veya proxy IP'si)
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// JWT token'dan mevcut kullanıcının ID'sini al
    /// </summary>
    /// <param name="user">ClaimsPrincipal (Controller.User)</param>
    /// <returns>Kullanıcı ID'si</returns>
    /// <exception cref="UnauthorizedAccessException">Geçersiz veya eksik token</exception>
    public static int GetCurrentUserId(ClaimsPrincipal user)
    {
        if (user == null)
            throw new UnauthorizedAccessException("User principal is null");

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("Invalid user token");

        return userId;
    }
}
