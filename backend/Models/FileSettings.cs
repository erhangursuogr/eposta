namespace DeuEposta.Models;

/// <summary>
/// File upload configuration settings from appsettings.json
/// </summary>
public class FileSettings
{
    public string UploadPath { get; set; } = "uploads";
    public int MaxFileSizeMB { get; set; } = 10;
    public string AllowedExtensions { get; set; } = string.Empty;
    public string DangerousExtensions { get; set; } = string.Empty;

    /// <summary>
    /// Get dangerous extensions as array
    /// </summary>
    public string[] GetDangerousExtensionsArray()
    {
        if (string.IsNullOrWhiteSpace(DangerousExtensions))
            return Array.Empty<string>();

        return DangerousExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().ToLowerInvariant())
            .ToArray();
    }
}