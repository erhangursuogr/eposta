using Hangfire.Dashboard;

namespace DeuEposta.Middleware;

/// <summary>
/// Hangfire Dashboard için authorization filtresi
/// Sadece authenticated kullanıcılar erişebilir
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Development ortamında herkes erişebilir
        var isDevelopment = httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();

        if (isDevelopment)
        {
            return true; // Development'ta serbest erişim
        }

        // Production'da authentication + ADMIN rolü gerekli
        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated)
        {
            return false;
        }

        // Sadece ADMIN rolüne sahip kullanıcılar erişebilir
        return httpContext.User.IsInRole("ADMIN");
    }
}