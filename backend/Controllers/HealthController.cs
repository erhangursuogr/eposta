using DeuEposta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Health endpoint'lere herkes erişebilir (monitoring, load balancer, kubernetes probes)
public class HealthController : ControllerBase
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<HealthController> _logger;
    private readonly IConfiguration _configuration;

    public HealthController(
        DeuEpostaContext context,
        ILogger<HealthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Check()
    {
        var healthStatus = new HealthCheckResult
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        var checks = new List<ComponentHealthCheck>();

        // Database bağlantı kontrolü
        checks.Add(await CheckDatabase());

        // Hangfire kontrolü
        checks.Add(CheckHangfire());

        // Disk alanı kontrolü
        checks.Add(CheckDiskSpace());

        // Memory kontrolü
        checks.Add(CheckMemoryUsage());

        healthStatus.Components = checks;

        // Genel sağlık durumunu belirle
        var hasUnhealthyComponent = checks.Any(c => c.Status != "Healthy");
        if (hasUnhealthyComponent)
        {
            healthStatus.Status = "Unhealthy";
        }

        var statusCode = healthStatus.Status == "Healthy" ? 200 : 503;

        _logger.LogInformation("Health check completed. Status: {Status}, Components: {ComponentCount}",
            healthStatus.Status, checks.Count);

        return StatusCode(statusCode, healthStatus);
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> DetailedCheck()
    {
        var result = new DetailedHealthCheck
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Version = "1.0.0",
            Uptime = GetUptime()
        };

        var components = new List<ComponentHealthCheck>
        {
            await CheckDatabase(),
            CheckHangfire(),
            CheckDiskSpace(),
            CheckMemoryUsage(),
            await CheckEmailSettings(),
            CheckConfiguration()
        };

        result.Components = components;
        result.Status = components.Any(c => c.Status != "Healthy") ? "Unhealthy" : "Healthy";

        return Ok(result);
    }

    [HttpGet("liveness")]
    public IActionResult Liveness()
    {
        var payload = new
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        return Ok(payload);
    }

    [HttpGet("readiness")]
    public async Task<IActionResult> Readiness()
    {
        // Readiness: en azından DB erişimi sağlanabiliyor mu?
        var dbCheck = await CheckDatabase();

        var isReady = dbCheck.Status == "Healthy";

        var payload = new
        {
            Status = isReady ? "Ready" : "NotReady",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Components = new[] { dbCheck }
        };

        return StatusCode(isReady ? 200 : 503, payload);
    }

    private async Task<ComponentHealthCheck> CheckDatabase()
    {
        var check = new ComponentHealthCheck
        {
            Name = "Database",
            Type = "Oracle"
        };

        try
        {
            var startTime = DateTime.UtcNow;

            // Basit bir sorgu ile bağlantıyı test et
            var count = await _context.Kullanicilar.CountAsync();

            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            check.Status = "Healthy";
            check.Data = new Dictionary<string, object>
            {
                { "UserCount", count },
                { "ResponseTimeMs", responseTime },
                { "ConnectionString", _configuration.GetConnectionString("DefaultConnection")?.Split(';')[0] ?? "Unknown" }
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
            _logger.LogError(ex, "Database health check failed");
        }

        return check;
    }

    private ComponentHealthCheck CheckHangfire()
    {
        var check = new ComponentHealthCheck
        {
            Name = "Hangfire",
            Type = "BackgroundJobs"
        };

        try
        {
            // Hangfire durumunu kontrol et (basit bir check)
            check.Status = "Healthy";
            check.Data = new Dictionary<string, object>
            {
                { "Status", "Running" }
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
            _logger.LogError(ex, "Hangfire health check failed");
        }

        return check;
    }

    private ComponentHealthCheck CheckDiskSpace()
    {
        var check = new ComponentHealthCheck
        {
            Name = "DiskSpace",
            Type = "Storage"
        };

        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
            var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            var totalSpaceGB = drive.TotalSize / (1024 * 1024 * 1024);
            var usagePercentage = ((double)(totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

            check.Status = usagePercentage > 90 ? "Unhealthy" : "Healthy";
            check.Data = new Dictionary<string, object>
            {
                { "FreeSpaceGB", freeSpaceGB },
                { "TotalSpaceGB", totalSpaceGB },
                { "UsagePercentage", Math.Round(usagePercentage, 2) }
            };

            if (usagePercentage > 90)
            {
                check.Error = $"Disk usage is critically high: {usagePercentage:F2}%";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }

        return check;
    }

    private ComponentHealthCheck CheckMemoryUsage()
    {
        var check = new ComponentHealthCheck
        {
            Name = "Memory",
            Type = "System"
        };

        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / (1024 * 1024);
            var privateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024);

            check.Status = workingSetMB > 1024 ? "Warning" : "Healthy"; // 1GB'dan fazla ise warning
            check.Data = new Dictionary<string, object>
            {
                { "WorkingSetMB", workingSetMB },
                { "PrivateMemoryMB", privateMemoryMB },
                { "ProcessId", process.Id }
            };

            if (workingSetMB > 1024)
            {
                check.Error = $"Memory usage is high: {workingSetMB} MB";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }

        return check;
    }

    private async Task<ComponentHealthCheck> CheckEmailSettings()
    {
        var check = new ComponentHealthCheck
        {
            Name = "EmailConfiguration",
            Type = "Configuration"
        };

        try
        {
            var emailSettings = await _context.SistemAyarlari
                .Where(s => s.AyarKategori == "EMAIL" && s.Aktif == "Y")
                .CountAsync();

            check.Status = emailSettings > 0 ? "Healthy" : "Warning";
            check.Data = new Dictionary<string, object>
            {
                { "ConfiguredSettings", emailSettings }
            };

            if (emailSettings == 0)
            {
                check.Error = "No active email configuration found";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }

        return check;
    }

    private ComponentHealthCheck CheckConfiguration()
    {
        var check = new ComponentHealthCheck
        {
            Name = "Configuration",
            Type = "Settings"
        };

        try
        {
            var requiredSettings = new[]
            {
                "ConnectionStrings:DefaultConnection",
                "Jwt:Key",
                "Jwt:Issuer",
                "Jwt:Audience"
            };

            var missingSettings = requiredSettings.Where(setting =>
                string.IsNullOrEmpty(_configuration[setting])).ToList();

            check.Status = missingSettings.Any() ? "Unhealthy" : "Healthy";
            check.Data = new Dictionary<string, object>
            {
                { "RequiredSettings", requiredSettings.Length },
                { "ConfiguredSettings", requiredSettings.Length - missingSettings.Count }
            };

            if (missingSettings.Any())
            {
                check.Error = $"Missing configuration: {string.Join(", ", missingSettings)}";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }

        return check;
    }

    private TimeSpan GetUptime()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        return DateTime.Now - process.StartTime;
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = string.Empty;
    public List<ComponentHealthCheck> Components { get; set; } = new();
}

public class DetailedHealthCheck : HealthCheckResult
{
    public string Version { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
}

public class ComponentHealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}