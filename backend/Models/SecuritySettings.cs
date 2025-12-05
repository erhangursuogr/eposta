namespace DeuEposta.Models;

public class SecuritySettings
{
    public string[] AllowedEmailDomains { get; set; } = { "deu.edu.tr" };
    public string[] BlacklistedIPs { get; set; } = Array.Empty<string>();
    public string[] WhitelistedIPs { get; set; } = Array.Empty<string>();
    public int MaxLoginAttempts { get; set; } = 5;
    public int LoginBlockDurationMinutes { get; set; } = 60;
    public int MaxDailyAnnouncements { get; set; } = 5;
    public int MaxRecipientsPerAnnouncement { get; set; } = 1000;
    public long MaxFileUploadSizeMB { get; set; } = 10;

    public string[] AllowedFileExtensions { get; set; } =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx",
        ".xls", ".xlsx", ".ppt", ".pptx", ".txt"
    };

    public bool EnableCaptchaAfterFailedAttempts { get; set; } = true;
    public int CaptchaRequiredAfterAttempts { get; set; } = 3;
    public bool LogSuspiciousActivity { get; set; } = true;
    public bool EnableRealTimeBlocking { get; set; } = true;
}