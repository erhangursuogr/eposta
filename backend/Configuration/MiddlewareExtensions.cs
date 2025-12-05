using DeuEposta.Middleware;
using Hangfire;

namespace DeuEposta.Configuration;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Middleware pipeline yapılandırması
    /// </summary>
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // OpenAPI/Swagger - Available in all environments but requires auth in production
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            // Production: Require authentication for OpenAPI
            app.MapOpenApi().RequireAuthorization();
        }

        // HTTPS Redirect (sadece production'da)
        if (app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }

        // Exception Handling (en erken)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Security Middleware
        app.UseMiddleware<SecurityMiddleware>();

        // CORS - Environment-based filtering
        var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:4200" };

        // Production'da localhost'ları filtrele
        if (app.Environment.IsProduction())
        {
            allowedOrigins = allowedOrigins
                .Where(origin => !origin.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (allowedOrigins.Length == 0)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("No production CORS origins configured. CORS will be disabled.");
            }
        }

        if (allowedOrigins.Length > 0)
        {
            app.UseCors(policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        }

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Request Logging (authentication'dan sonra - user bilgisi için)
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Static Files (Frontend - wwwroot) - sadece klasör varsa
        var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        if (Directory.Exists(wwwrootPath))
        {
            app.UseStaticFiles();
        }
        else
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("wwwroot folder not found at {Path}. Static files disabled.", wwwrootPath);
        }

        // Rate Limiting
        app.UseRateLimiter();

        // Hangfire Dashboard
        ConfigureHangfireDashboard(app);

        // Controllers & Health Checks
        app.MapControllers();

        // Health check endpoints
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    duration = report.TotalDuration,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration,
                        data = e.Value.Data,
                        exception = e.Value.Exception?.Message,
                        tags = e.Value.Tags
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        // Simple readiness check
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // Angular routing fallback - Frontend için (wwwroot/index.html) - sadece varsa
        if (Directory.Exists(wwwrootPath))
        {
            app.MapFallbackToFile("index.html");
        }

        return app;
    }

    /// <summary>
    /// Hangfire Dashboard yapılandırması
    /// </summary>
    private static void ConfigureHangfireDashboard(WebApplication app)
    {
        var dashboardEnabled = app.Configuration.GetValue<bool>("Hangfire:Dashboard:Enabled", true);
        var requireAuth = app.Configuration.GetValue<bool>("Hangfire:Dashboard:RequireAuthentication", true);

        if (dashboardEnabled)
        {
            var dashboardOptions = new DashboardOptions();

            if (requireAuth)
            {
                dashboardOptions.Authorization = new[] { new HangfireAuthorizationFilter() };
            }

            app.UseHangfireDashboard("/hangfire", dashboardOptions);

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Hangfire Dashboard enabled at /hangfire (Auth required: {AuthRequired})", requireAuth);
        }
    }
}