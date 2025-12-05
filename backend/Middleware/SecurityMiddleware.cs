using System.Text;

namespace DeuEposta.Middleware;

public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIP = GetClientIP(context);
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        // requestPath removed (unused)
        var isDevelopment = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);

        // 1. Basit CSRF koruması (state-changing operations için)
        if (IsStateChangingRequest(context) && !IsValidCsrfRequest(context))
        {
            _logger.LogWarning("Potential CSRF attack detected from IP: {IP}, Path: {Path}", clientIP, context.Request.Path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden: Invalid request origin");
            return;
        }

        // 2. Suspicious User-Agent kontrolü
        if (IsSuspiciousUserAgent(userAgent))
        {
            // Sadece logla; engelleme/blacklist artışı yapma
            _logger.LogWarning("Suspicious User-Agent detected: {UserAgent} from IP: {IP}", userAgent, clientIP);
        }

        // 3. Request boyut kontrolü (DoS koruması) - Body okumadan ÖNCE
        var contentLength = context.Request.ContentLength;
        // Read maximum request size from configuration (FileSettings:MaxFileSizeMB) with a sensible default for small internal usage
        var maxFileMb = _configuration.GetValue<int>("FileSettings:MaxFileSizeMB", 50);
        long maxRequestSizeBytes = maxFileMb * 1024L * 1024L;
        if (contentLength.HasValue && contentLength.Value > maxRequestSizeBytes)
        {
            _logger.LogWarning("Oversized request from IP: {IP}, Size: {Size} bytes", clientIP, contentLength);
            context.Response.StatusCode = 413;
            await context.Response.WriteAsync("Request too large");
            return;
        }

        // Rate limiting logic has been removed in favor of built-in ASP.NET RateLimiter

        // 5. SQL Injection ve XSS koruması (log-only)
        await LogSuspiciousContentIfAny(context);

        // 6. Security Headers ekle
        AddSecurityHeaders(context);

        await _next(context);
    }

    private string GetClientIP(HttpContext context)
    {
        // Proxy arkasındaki gerçek IP'yi al
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            return realIP;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    // IP blacklist özelliği kaldırıldı

    private bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return true;

        // For small internal deployments we relax UA checks to reduce false positives (allow common tools like curl/postman)
        var suspiciousPatterns = new[]
        {
            "sqlmap", "nikto", "nmap", "dirbuster",
            "burpsuite", "owasp", "hacker", "exploit", "payload",
            "python-requests"
        };

        return suspiciousPatterns.Any(pattern => userAgent.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // CheckRateLimit method removed as it is no longer needed

    private async Task LogSuspiciousContentIfAny(HttpContext context)
    {
        if (context.Request.ContentLength == 0) return;

        try
        {
            context.Request.EnableBuffering();
            // multipart/form-data ise ve büyük upload'lar için tarama atlanır
            var contentType = context.Request.ContentType ?? string.Empty;
            if (contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Body.Position = 0;
                return;
            }

            // Yalnızca text/* ve application/json gibi içeriklerde hafif tarama yapalım
            if (!(contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                  contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)))
            {
                context.Request.Body.Position = 0;
                return;
            }

            // En fazla 64KB oku (kademeli tarama) – büyük body'lerde tam okuma yapma
            const int maxScanBytes = 64 * 1024;
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: maxScanBytes, leaveOpen: true);
            char[] buffer = new char[maxScanBytes];
            int read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            var body = new string(buffer, 0, read);
            context.Request.Body.Position = 0;

            var suspiciousPatterns = new[]
            {
                // High-risk patterns only (reduced sensitivity for small internal usage)
                "union select", "drop table", "delete from",
                "<script", "javascript:",
                "../", "..\\"
            };

            var queryString = context.Request.QueryString.ToString().ToLower();
            var combinedContent = (body + queryString).ToLower();
            if (suspiciousPatterns.Any(pattern => combinedContent.Contains(pattern)))
            {
                _logger.LogWarning("Suspicious content pattern detected on {Path}", context.Request.Path);
            }
        }
        catch
        {
            // Tarama başarısızsa engelleme yapma
        }
    }

    private bool IsStateChangingRequest(HttpContext context)
    {
        var method = context.Request.Method.ToUpper();
        return method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH";
    }

    private bool IsValidCsrfRequest(HttpContext context)
    {
        var isDevelopment = _configuration.GetValue<string>("Environment") == "Development";

        // Development'ta CSRF kontrolünü atla
        if (isDevelopment)
            return true;

        // Login endpoint'ini atla (ilk giriş için gerekli)
        if (context.Request.Path.StartsWithSegments("/api/auth/login"))
            return true;

        // Health check endpoint'ini atla
        if (context.Request.Path.StartsWithSegments("/api/health"))
            return true;

        // Temel origin kontrolü
        var origin = context.Request.Headers["Origin"].FirstOrDefault();
        var referer = context.Request.Headers["Referer"].FirstOrDefault();
        var host = context.Request.Headers["Host"].FirstOrDefault();

        // İzin verilen origin'ler
        var allowedOrigins = new[]
        {
            "http://localhost:3000",   // React dev
            "http://localhost:5173",   // Vite dev
            "https://localhost:5118",  // HTTPS local
            "http://localhost:5118",   // HTTP local
            "https://kurumsalduyuru.deu.edu.tr"  // Production
        };

        // Origin kontrolü
        if (!string.IsNullOrEmpty(origin))
        {
            return allowedOrigins.Any(allowed =>
                string.Equals(origin, allowed, StringComparison.OrdinalIgnoreCase));
        }

        // Referer kontrolü (fallback)
        if (!string.IsNullOrEmpty(referer))
        {
            return allowedOrigins.Any(allowed =>
                referer.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
        }

        // Same-origin request kontrolü (API tools için)
        if (!string.IsNullOrEmpty(host))
        {
            var allowedHosts = new[] { "localhost:5118", "localhost:3000", "localhost:5173", "kurumsalduyuru.deu.edu.tr" };
            return allowedHosts.Any(allowed =>
                string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
            response.Headers["X-Content-Type-Options"] = "nosniff";

        if (!response.Headers.ContainsKey("X-Frame-Options"))
            response.Headers["X-Frame-Options"] = "DENY";

        if (!response.Headers.ContainsKey("X-XSS-Protection"))
            response.Headers["X-XSS-Protection"] = "1; mode=block";

        if (!response.Headers.ContainsKey("Referrer-Policy"))
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        if (!response.Headers.ContainsKey("Content-Security-Policy"))
            response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com data:; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self';";
    }

    // TrackSuspiciousActivity and _failedAttempts removed: suspicious activity is logged but not counted

    // RateLimitInfo class removed in favor of built-in ASP.NET RateLimiter
}