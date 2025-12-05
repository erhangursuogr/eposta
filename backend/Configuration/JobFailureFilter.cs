using DeuEposta.Data;
using DeuEposta.Services;
using Hangfire.Server;

namespace DeuEposta.Configuration;

/// <summary>
/// Hangfire job hatalarını yakalayıp admin'e email bildirimi gönderen filter
/// </summary>
public class JobFailureFilter : IServerFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobFailureFilter> _logger;

    public JobFailureFilter(
        IServiceProvider serviceProvider,
        ILogger<JobFailureFilter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        // İşlem başlamadan önce bir şey yapmaya gerek yok
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        // Job başarısız olduysa admin'lere bildirim gönder
        if (filterContext.Exception != null)
        {
            try
            {
                var jobName = filterContext.BackgroundJob.Job.Type.Name;
                var methodName = filterContext.BackgroundJob.Job.Method.Name;
                var jobId = filterContext.BackgroundJob.Id;
                var exception = filterContext.Exception;

                // Email servisi ve DB context scope içinde alınmalı (DI lifetime scope)
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<DeuEpostaContext>();
                var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

                // ADMIN yetkisine sahip tüm kullanıcıların email adreslerini al
                var adminEmails = dbContext.Kullanicilar
                    .Where(k => k.RolId == 1 && k.Aktif == "Y" && !string.IsNullOrEmpty(k.Email))
                    .Select(k => k.Email)
                    .ToList();

                if (adminEmails.Count == 0)
                {
                    _logger.LogWarning("ADMIN yetkisine sahip aktif kullanıcı bulunamadı, job failure bildirimi gönderilemedi");
                    return;
                }

                var emailBody = $@"
<h2 style='color: #d32f2f;'>⚠️ Hangfire Job Başarısız Oldu</h2>

<table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
    <tr style='background-color: #f5f5f5;'>
        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Job ID</strong></td>
        <td style='padding: 10px; border: 1px solid #ddd;'>{jobId}</td>
    </tr>
    <tr>
        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Job Class</strong></td>
        <td style='padding: 10px; border: 1px solid #ddd;'>{jobName}</td>
    </tr>
    <tr style='background-color: #f5f5f5;'>
        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Method</strong></td>
        <td style='padding: 10px; border: 1px solid #ddd;'>{methodName}</td>
    </tr>
    <tr>
        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Hata Zamanı</strong></td>
        <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.Now:dd.MM.yyyy HH:mm:ss}</td>
    </tr>
    <tr style='background-color: #f5f5f5;'>
        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Hata Mesajı</strong></td>
        <td style='padding: 10px; border: 1px solid #ddd; color: #d32f2f;'>{exception.Message}</td>
    </tr>
</table>

<h3>Stack Trace:</h3>
<pre style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #d32f2f; overflow-x: auto; font-size: 12px;'>{exception.StackTrace}</pre>

<hr style='margin: 30px 0;'/>

<p style='color: #666; font-size: 12px;'>
    <strong>Not:</strong> Hangfire dashboard'dan detaylı bilgi alabilirsiniz:
    <a href='https://kurumsalduyuru.deu.edu.tr/hangfire/jobs/details/{jobId}'>Job #{jobId}</a>
</p>

<p style='color: #999; font-size: 11px; margin-top: 30px;'>
    Bu otomatik bir bildirimdir. DEÜ Eposta Yönetim Sistemi - Hangfire Job Monitor
</p>
";

                // Async email gönderimi - Tüm admin'lere BCC ile gönder
                _ = emailService.SendEmailAsync(new SendEmailRequest
                {
                    BccRecipients = adminEmails,
                    Subject = $"🚨 [HANGFIRE] Job Başarısız: {methodName}",
                    Body = emailBody,
                    IsHtml = true,
                    Category = "EMAIL_SISTEM"
                });

                _logger.LogError(
                    exception,
                    "Hangfire job başarısız oldu. {AdminCount} admin'e bildirim gönderildi. JobId: {JobId}, JobName: {JobName}, Method: {MethodName}",
                    adminEmails.Count, jobId, jobName, methodName
                );

                // Audit log kaydı
                auditLog.LogAsync(
                kategori: "SYSTEM",
                islem: "HANGFIRE_JOB_FAILURE",
                detay: $"JobId: {jobId}, JobName: {jobName}, Method: {methodName}, Hata: {exception.Message}",
                kullaniciId: null
                ).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JobFailureFilter'da hata oluştu, admin'e bildirim gönderilemedi");
            }
        }
    }
}