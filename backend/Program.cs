using DeuEposta.Configuration;
using Hangfire;
using Serilog;
using Serilog.Events;

// =============================================
// SERILOG CONFIGURATION
// =============================================
var isProduction = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);

IConfiguration configuration;
try
{
    configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: Configuration loading failed: {ex.Message}");
    Console.Error.WriteLine("Please ensure appsettings.json exists and is valid JSON.");
    Environment.Exit(1);
    throw; // Never reached but satisfies compiler
}

var seqServerUrl = configuration["Serilog:SeqServerUrl"];
var seqApiKey = configuration["Serilog:SeqApiKey"];

var serilogConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Destructure.ByTransforming<Microsoft.AspNetCore.Http.IFormFile>(f => new { f.FileName, f.Length, f.ContentType })
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

// Seq sink (if configured)
if (!string.IsNullOrEmpty(seqServerUrl))
{
    if (!string.IsNullOrEmpty(seqApiKey))
        serilogConfig = serilogConfig.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
    else
        serilogConfig = serilogConfig.WriteTo.Seq(seqServerUrl);
}

// File logging (her zaman aktif)
serilogConfig = serilogConfig.WriteTo.File(
    "logs/deu-eposta-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30,
    fileSizeLimitBytes: 10485760,
    rollOnFileSizeLimit: true,
    shared: true,
    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}");

Log.Logger = serilogConfig.CreateLogger();

// =============================================
// APPLICATION BUILDER
// =============================================
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// =============================================
// SERVICE CONFIGURATION
// =============================================
builder.Services.AddFileUploadConfiguration(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHangfireConfiguration(builder.Configuration);
builder.Services.AddRateLimitingConfiguration();
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddApiConfiguration();

// =============================================
// BUILD & CONFIGURE PIPELINE
// =============================================
var app = builder.Build();

app.ConfigureMiddlewarePipeline();

// =============================================
// HANGFIRE JOB FAILURE FILTER
// =============================================
// Job başarısız olduğunda admin'e email bildirimi gönder
var jobFailureFilter = app.Services.GetRequiredService<JobFailureFilter>();
Hangfire.GlobalJobFilters.Filters.Add(jobFailureFilter);

// =============================================
// HANGFIRE RECURRING JOBS
// =============================================
// NOTE: Zamanlama işlemleri artık ScheduleService üzerinden yönetiliyor
// Her bir zamanlama kendi Hangfire job'ını oluşturuyor
// Recurring job'lara ihtiyaç duyulursa buraya eklenebilir

// ORPHAN FILE CLEANUP: 7+ günlük bağlanmamış dosyaları temizle
RecurringJob.AddOrUpdate<DeuEposta.Jobs.OrphanFileCleanupJob>(
    "orphan-file-cleanup",
    job => job.CleanupOrphanFilesAsync(),
    "0 3 * * 0", // Her Pazar gece 03:00 (Cron: dakika saat gün ay haftanın_günü, 0=Pazar)
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

// =============================================
// RUN APPLICATION
// =============================================
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting DEÜ Duyuru Yönetim Sistemi");
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
}
