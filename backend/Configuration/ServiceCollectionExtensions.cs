using DeuEposta.Data;
using DeuEposta.Jobs;
using DeuEposta.Mapping;
using DeuEposta.Models;
using DeuEposta.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace DeuEposta.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Tüm uygulama servislerini ekler
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Context - Environment variable fallback
        var isProduction = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);
        var connectionString = Environment.GetEnvironmentVariable("EPOSTA_DB_CONNECTION")
                              ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured. Set via appsettings.json or EPOSTA_DB_CONNECTION environment variable.");

        // SECURITY WARNING: Production'da environment variable önerilir (appsettings.json'da connection string güvenli değildir)
        if (isProduction && Environment.GetEnvironmentVariable("EPOSTA_DB_CONNECTION") == null)
        {
            Console.WriteLine("⚠️  WARNING: Production ortamında EPOSTA_DB_CONNECTION environment variable kullanılmalı. Appsettings.json'dan bağlantı alınıyor (GÜVENLİK RİSKİ!)");
        }

        services.AddDbContext<DeuEpostaContext>(options => options.UseOracle(connectionString));

        // Memory Cache
        services.AddMemoryCache();

        // HttpContextAccessor (required for AuditLogService to access HttpContext)
        services.AddHttpContextAccessor();

        // HttpClient (SSO Keycloak için gerekli)
        services.AddHttpClient();

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Application Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailCategoryService, EmailCategoryService>();
        services.AddScoped<ILdapService, LdapService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IAnnouncementOperationsService, AnnouncementOperationsService>(); // REFACTORING: İşlem metodları ayrıldı
        services.AddScoped<IAnnouncementApprovalService, AnnouncementApprovalService>();
        services.AddScoped<IAnnouncementApprovalWorkflowService, AnnouncementApprovalWorkflowService>();
        services.AddScoped<IAnnouncementApprovalNotificationService, AnnouncementApprovalNotificationService>();
        services.AddScoped<IAnnouncementRecipientService, AnnouncementRecipientService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ITemplateCategoryService, TemplateCategoryService>();
        services.AddScoped<IEmailGroupService, EmailGroupService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IOracle11gService, Oracle11gService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ILogService, LogService>();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

        // Hangfire Jobs
        services.AddScoped<OrphanFileCleanupJob>();

        // Data Protection - Persistent keys (DPAPI disabled for production compatibility)
        // DPAPI removed: IIS app pool identity permissions sorunlarını önler
        // SECURITY: keys klasörü permission'ını sıkılaştırın (sadece app pool identity okusun)
        var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "keys");
        Directory.CreateDirectory(keysPath); // Ensure directory exists

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("DeuEpostaYonetim");

        return services;
    }

    /// <summary>
    /// JWT Authentication yapılandırması
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];
        var jwtKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtIssuer))
            throw new InvalidOperationException("JWT Issuer is not configured");
        if (string.IsNullOrWhiteSpace(jwtAudience))
            throw new InvalidOperationException("JWT Audience is not configured");
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is not configured");

        // GÜVENLIK: JWT Key minimum 256 bit (32 karakter) olmalı
        if (jwtKey.Length < 32)
            throw new InvalidOperationException("JWT Key must be at least 256 bits (32 characters) for security. Current length: " + jwtKey.Length);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "JWT Authentication failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("JWT Challenge: {Error} - {ErrorDescription}", context.Error ?? "No error", context.ErrorDescription ?? "No description");

                        // 401 Unauthorized için JSON response
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var response = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            statusCode = 401,
                            message = "Kimlik doğrulama başarısız. Lütfen giriş yapınız.",
                            data = new
                            {
                                error = context.Error ?? "unauthorized",
                                errorDescription = context.ErrorDescription ?? "Token eksik veya geçersiz"
                            }
                        });

                        return context.Response.WriteAsync(response);
                    },
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                        // Skip Hangfire
                        if (path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase))
                            return Task.CompletedTask;

                        // SECURITY: Cookie'den token al (HttpOnly cookie support)
                        if (context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                        {
                            context.Token = cookieToken;
                            // Removed verbose logging - runs on every request
                        }
                        else
                        {
                            // Fallback: Authorization header'dan al (backward compatibility)
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            // Removed verbose logging - runs on every request
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var jti = context.Principal?.FindFirst("jti")?.Value;

                        // Only log blacklisted tokens (security issue) - not every valid request
                        if (!string.IsNullOrEmpty(jti) && blacklistService.IsTokenBlacklisted(jti))
                        {
                            logger.LogWarning("Token {Jti} is blacklisted - rejecting request", jti);
                            context.Fail("Token has been revoked");
                        }

                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";

                        var userRole = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";
                        var userName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown";

                        var response = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            statusCode = 403,
                            message = "Bu işlem için yetkiniz bulunmamaktadır.",
                            data = new
                            {
                                userRole = userRole,
                                userName = userName,
                                requiredRoles = "Bu endpoint için yeterli yetkiniz yok"
                            }
                        });

                        return context.Response.WriteAsync(response);
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Hangfire job scheduler yapılandırması
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireStorageDir = configuration.GetValue<string>("Hangfire:StoragePath")
            ?? Path.Combine(AppContext.BaseDirectory, "data", "hangfire");

        if (!Directory.Exists(hangfireStorageDir))
            Directory.CreateDirectory(hangfireStorageDir);

        var hangfireDbPath = Path.Combine(hangfireStorageDir, "hangfire.db");

        services.AddHangfire(config => config.UseSQLiteStorage(hangfireDbPath));
        services.AddHangfireServer();

        // Job failure notification filter - Admin'e başarısız job bildirimi gönder
        services.AddSingleton<JobFailureFilter>();

        return services;
    }

    /// <summary>
    /// Rate limiting yapılandırması
    /// </summary>
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        const int ApiPermitLimit = 30;
        const int LoginPermitLimit = 10;
        const int UploadPermitLimit = 3;
        const int GlobalPermitLimit = 60;
        const int ApiQueueLimit = 5;
        const int LoginQueueLimit = 2;
        const int UploadQueueLimit = 1;
        const int GlobalQueueLimit = 10;
        const int WindowMinutes = 1;

        services.AddRateLimiter(options =>
        {
            // Rate limit aşıldığında Retry-After header ekle
            options.OnRejected = async (context, token) =>
            {
                // IMPORTANT: Response'u handle ettiğimizi belirt (queue'ya alınmasın)
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                }

                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.Headers["Retry-After"] = ((int)WindowMinutes * 60).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    statusCode = 429,
                    message = $"Çok fazla istek gönderdiniz. Lütfen {WindowMinutes * 60} saniye bekleyip tekrar deneyin.",
                    retryAfter = WindowMinutes * 60
                }, cancellationToken: token);
            };

            // Genel API rate limiting
            options.AddFixedWindowLimiter("Api", limiterOptions =>
            {
                limiterOptions.PermitLimit = ApiPermitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(WindowMinutes);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = ApiQueueLimit;
            });

            // Login endpoint rate limiting (SECURITY: Brute force koruması - queue yok)
            options.AddFixedWindowLimiter("Login", limiterOptions =>
            {
                limiterOptions.PermitLimit = LoginPermitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(WindowMinutes);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0; // SECURITY: Login istekleri kuyruğa alınmaz, direkt red
            });

            // Upload endpoint rate limiting
            options.AddFixedWindowLimiter("Upload", limiterOptions =>
            {
                limiterOptions.PermitLimit = UploadPermitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(WindowMinutes);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = UploadQueueLimit;
            });

            // Global rate limiter - user ID veya IP bazlı
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var userId = httpContext.User?.FindFirst("nameid")?.Value ??
                             httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var partitionKey = !string.IsNullOrEmpty(userId)
                    ? $"user:{userId}"
                    : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = GlobalPermitLimit,
                    Window = TimeSpan.FromMinutes(WindowMinutes),
                    QueueLimit = GlobalQueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });

        return services;
    }

    /// <summary>
    /// File upload limiti yapılandırması
    /// </summary>
    public static IServiceCollection AddFileUploadConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var maxFileSizeBytes = string.IsNullOrWhiteSpace(connectionString)
            ? 50 * 1024 * 1024L
            : DatabaseSettingsLoader.GetMaxFileSizeBytes(connectionString);

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxFileSizeBytes;
        });

        // FileSettings configuration
        services.Configure<FileSettings>(configuration.GetSection("FileSettings"));

        return services;
    }

    /// <summary>
    /// Controllers ve API yapılandırması
    /// </summary>
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        services.AddOpenApi();
        services.AddCors();

        return services;
    }

    /// <summary>
    /// Health Checks yapılandırması
    /// </summary>
    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("EPOSTA_DB_CONNECTION")
                              ?? configuration.GetConnectionString("DefaultConnection");

        var smtpServer = configuration["EmailSettings:SmtpServer"] ?? "localhost";
        var smtpPort = configuration.GetValue<int>("EmailSettings:SmtpPort", 25);
        var uploadsPath = configuration.GetValue<string>("FileSettings:UploadPath") ?? "uploads";

        services.AddHealthChecks()
            // Database Health Check
            .AddOracle(
                connectionString: connectionString ?? throw new InvalidOperationException("Database connection string not configured"),
                name: "database",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                tags: new[] { "db", "oracle", "ready" })
            // Hangfire Health Check
            .AddHangfire(
                setup: options => { },
                name: "hangfire",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "hangfire", "jobs" })
            // SMTP Health Check
            .AddCheck(
                "smtp",
                new DeuEposta.HealthChecks.SmtpHealthCheck(smtpServer, smtpPort),
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "smtp", "email" })
            // Disk Space Health Check
            .AddCheck(
                "disk",
                new DeuEposta.HealthChecks.DiskSpaceHealthCheck(uploadsPath, 1_073_741_824), // 1GB threshold
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "disk", "storage" });

        return services;
    }
}