using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Utils;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DeuEposta.Services;

public interface IAuditLogService
{
    Task LogAsync(string kategori, string islem, string? detay, int? kullaniciId = null, string? ipAdres = null, string? userAgent = null, string logSeviye = "INFO", bool sendToCentral = false);
}

public class AuditLogService : IAuditLogService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _centralLogConnectionString;

    // Merkezi log sabitleri
    private const string APP_NAME = "KURUMSAL_DUYURU";
    private const string APP_MODULE = "AUTH_SERVICE";

    public AuditLogService(
        DeuEpostaContext context,
        ILogger<AuditLogService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        // Merkezi log bağlantısı (opsiyonel - yoksa merkezi loglama devre dışı)
        _centralLogConnectionString = Environment.GetEnvironmentVariable("EPOSTA_SYSLOG_CONNECTION")
                                     ?? Environment.GetEnvironmentVariable("EPOSTA_ORACLE11G_CONNECTION")
                                     ?? configuration.GetConnectionString("SyslogConnection")
                                     ?? configuration.GetConnectionString("Oracle11gConnection");
    }

    public async Task LogAsync(string kategori, string islem, string? detay, int? kullaniciId = null, string? ipAdres = null, string? userAgent = null, string logSeviye = "INFO", bool sendToCentral = false)
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

            // 1. Yerel veritabanına yaz (LOG_SISTEM)
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

            // 2. Merkezi log sunucusuna yaz (opsiyonel)
            if (sendToCentral)
            {
                await WriteToCentralLogAsync(kategori, islem, kullaniciId?.ToString(), ipAdres, detay);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing audit log: {Islem}", islem);
        }
    }

    /// <summary>
    /// Merkezi log sunucusuna (system.syslog_deu) log kaydı yazar
    /// </summary>
    private async Task WriteToCentralLogAsync(string kategori, string islem, string? userId, string? ipAddress, string? message)
    {
        if (string.IsNullOrEmpty(_centralLogConnectionString))
        {
            _logger.LogDebug("Central log connection not configured, skipping central log");
            return;
        }

        try
        {
            using var connection = new OracleConnection(_centralLogConnectionString);
            await connection.OpenAsync();

            using var command = new OracleCommand("system.syslog_deu", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 10;

            // Stored procedure parametreleri
            command.Parameters.Add("p_param1", OracleDbType.Varchar2, APP_NAME, ParameterDirection.Input);
            command.Parameters.Add("p_param2", OracleDbType.Varchar2, $"{APP_MODULE}_{kategori}", ParameterDirection.Input);
            command.Parameters.Add("p_param3", OracleDbType.Varchar2, islem ?? string.Empty, ParameterDirection.Input);
            command.Parameters.Add("p_param4", OracleDbType.Varchar2, userId ?? string.Empty, ParameterDirection.Input);
            command.Parameters.Add("p_param5", OracleDbType.Varchar2, ipAddress ?? string.Empty, ParameterDirection.Input);
            command.Parameters.Add("p_param6", OracleDbType.Varchar2, TruncateMessage(message, 4000), ParameterDirection.Input);

            await command.ExecuteNonQueryAsync();

            _logger.LogDebug("Central syslog recorded: {Kategori}/{Islem} - {UserId}", kategori, islem, userId);
        }
        catch (Exception ex)
        {
            // Merkezi log hatası ana işlemi engellemez
            _logger.LogWarning(ex, "Failed to write to central syslog: {Kategori}/{Islem}", kategori, islem);
        }
    }

    /// <summary>
    /// Mesajı Oracle VARCHAR2 limitine göre kırpar
    /// </summary>
    private static string TruncateMessage(string? message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= maxLength ? message : message[..maxLength];
    }
}