using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DeuEposta.HealthChecks;

public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly string _path;
    private readonly long _thresholdBytes;

    public DiskSpaceHealthCheck(string path, long thresholdBytes = 1_073_741_824) // Default 1GB
    {
        _path = path;
        _thresholdBytes = thresholdBytes;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Relative path ise working directory'den root al
            var fullPath = Path.IsPathRooted(_path) ? _path : Path.Combine(Directory.GetCurrentDirectory(), _path);
            var rootPath = Path.GetPathRoot(fullPath);

            // Root path boşsa C:\ kullan
            if (string.IsNullOrEmpty(rootPath))
                rootPath = "C:\\";

            var driveInfo = new DriveInfo(rootPath);

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Drive {driveInfo.Name} is not ready"));
            }

            var availableSpace = driveInfo.AvailableFreeSpace;
            var totalSpace = driveInfo.TotalSize;
            var usedSpace = totalSpace - availableSpace;
            var usedPercentage = (double)usedSpace / totalSpace * 100;

            var data = new Dictionary<string, object>
            {
                { "drive", driveInfo.Name },
                { "availableSpaceGB", Math.Round(availableSpace / 1024.0 / 1024.0 / 1024.0, 2) },
                { "totalSpaceGB", Math.Round(totalSpace / 1024.0 / 1024.0 / 1024.0, 2) },
                { "usedPercentage", Math.Round(usedPercentage, 2) }
            };

            if (availableSpace < _thresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Available disk space ({data["availableSpaceGB"]} GB) is below threshold",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Sufficient disk space available", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Error checking disk space: {ex.Message}"));
        }
    }
}