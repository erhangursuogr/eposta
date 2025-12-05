using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;

namespace DeuEposta.HealthChecks;

public class SmtpHealthCheck : IHealthCheck
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly int _timeoutSeconds;

    public SmtpHealthCheck(string smtpServer, int smtpPort, int timeoutSeconds = 5)
    {
        _smtpServer = smtpServer;
        _smtpPort = smtpPort;
        _timeoutSeconds = timeoutSeconds;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(_smtpServer, _smtpPort, cancellationToken).AsTask();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return HealthCheckResult.Degraded($"SMTP server connection timeout after {_timeoutSeconds}s");
            }

            await connectTask; // Await to get exceptions

            if (connectTask.IsFaulted && connectTask.Exception != null)
            {
                return HealthCheckResult.Unhealthy($"SMTP server connection failed: {connectTask.Exception.GetBaseException().Message}");
            }

            var data = new Dictionary<string, object>
            {
                { "server", _smtpServer },
                { "port", _smtpPort },
                { "connected", tcpClient.Connected }
            };

            return HealthCheckResult.Healthy("SMTP server is reachable", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Error checking SMTP health: {ex.Message}");
        }
    }
}