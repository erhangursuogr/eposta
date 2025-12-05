namespace DeuEposta.Models.DTOs;

public class SystemEmailSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
}

public class SystemSetting
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Gizli { get; set; } = "N"; // Y/N - Oracle compat
    public string Aktif { get; set; } = "Y"; // Y/N - Oracle compat
    public int? GorevYeri { get; set; }
}

public class UpdateEmailSettingsRequest
{
    public List<EmailSettingUpdate> Settings { get; set; } = new();
}

public class EmailSettingUpdate
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ManagerUserDto
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class EmailCategoryDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool HasSignature { get; set; }
}

public class CreateSystemSettingRequest
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSecret { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int? GorevYeri { get; set; }
}

public class UpdateSystemSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? GorevYeri { get; set; }
}

public class BulkUpdateSettingRequest
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}