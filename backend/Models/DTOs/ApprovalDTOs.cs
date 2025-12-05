using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Models.DTOs;

public class ApprovalRequest
{
    [MaxLength(500)]
    public string? OnayNotu { get; set; }
}

public class RejectionRequest
{
    [Required, MaxLength(500)]
    public string RejectionNote { get; set; } = string.Empty;
}

public class CancellationRequest
{
    [Required, MaxLength(500)]
    public string CancellationReason { get; set; } = string.Empty;
}

// İki Aşamalı Onay için ek DTO

public class CoordinatorApproveRequest
{
    [Required]
    public int ManagerId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}