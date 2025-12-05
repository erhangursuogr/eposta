using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Utils;

namespace DeuEposta.Services;

public interface IAuditLogService
{
    Task LogAsync(string kategori, string islem, string? detay, int? kullaniciId = null, string? ipAdres = null, string? userAgent = null, string logSeviye = "INFO");
}

public class AuditLogService : IAuditLogService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        DeuEpostaContext context,
        ILogger<AuditLogService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string kategori, string islem, string? detay, int? kullaniciId = null, string? ipAdres = null, string? userAgent = null, string logSeviye = "INFO")
    {
        try
        {
            // Kullanıcı ID verilmediyse HttpContext'ten al (JWT claim'den)
            if (!kullaniciId.HasValue && _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    kullaniciId = HttpContextHelper.GetCurrentUserId(_httpContextAccessor.HttpContext.User);
                }
                catch (UnauthorizedAccessException)
                {
                    // Token geçersiz veya eksik - kullaniciId null kalacak
                }
            }

            // IP adresi verilmediyse HttpContext'ten al (proxy/load balancer desteği)
            if (string.IsNullOrEmpty(ipAdres) && _httpContextAccessor.HttpContext != null)
            {
                ipAdres = HttpContextHelper.GetClientIPAddress(_httpContextAccessor.HttpContext);
            }

            // User-Agent verilmediyse HttpContext'ten al
            if (string.IsNullOrEmpty(userAgent) && _httpContextAccessor.HttpContext != null)
            {
                userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
            }

            var log = new LogSistem
            {
                KullaniciId = kullaniciId,
                LogSeviye = logSeviye,
                Kategori = kategori,
                Islem = islem,
                Detay = detay,
                IpAdres = ipAdres,
                UserAgent = userAgent,
                LogTarihi = DateTime.Now
            };

            _context.LogSistem.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing audit log: {Islem}", islem);
        }
    }
}