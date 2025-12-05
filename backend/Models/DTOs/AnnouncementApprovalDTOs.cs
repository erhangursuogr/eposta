using System.Text.Json.Serialization;

namespace DeuEposta.Models.DTOs;

public class ApproveAnnouncementRequest
{
    public string? Note { get; set; }
}

public class RejectAnnouncementRequest
{
    [JsonPropertyName("redNedeni")]
    public string RejectionNote { get; set; } = string.Empty;
}

public class CancelAnnouncementRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}

public class RequestChangesRequest
{
    public string DegisiklikNotu { get; set; } = string.Empty;
}

public class ScheduleAnnouncementRequest
{
    public DateTime ScheduledDate { get; set; }
}