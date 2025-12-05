using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/announcement-approval")]
[Authorize]
public class AnnouncementApprovalController : ControllerBase
{
    private readonly IAnnouncementApprovalService _approvalService;
    private readonly IAnnouncementApprovalWorkflowService _workflowService;

    public AnnouncementApprovalController(
        IAnnouncementApprovalService approvalService,
        IAnnouncementApprovalWorkflowService workflowService)
    {
        _approvalService = approvalService;
        _workflowService = workflowService;
    }

    [HttpPost("{id}/submit")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<IActionResult> SubmitForApproval(int id)
    {
        var userId = GetCurrentUserId();

        var response = await _workflowService.SubmitForApprovalAsync(id, userId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            403 => StatusCode(403, response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("pending")]
    [Authorize(Roles = "ADMIN,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole(RolKodu.ADMIN);
        var isCoordinator = User.IsInRole(RolKodu.COORDINATOR);
        var isManager = User.IsInRole(RolKodu.MANAGER);

        var response = await _approvalService.GetPendingApprovalsAsync(page, pageSize, currentUserId, isAdmin, isCoordinator, isManager);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> ApproveAnnouncement(int id, [FromBody] ApproveAnnouncementRequest request)
    {
        var userId = GetCurrentUserId();

        var response = await _workflowService.ApproveAnnouncementAsync(id, userId, request.Note);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> RejectAnnouncement(int id, [FromBody] RejectAnnouncementRequest request)
    {
        var userId = GetCurrentUserId();

        var response = await _workflowService.RejectAnnouncementAsync(id, userId, request.RejectionNote);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> CancelAnnouncement(int id, [FromBody] CancelAnnouncementRequest request)
    {
        var userId = GetCurrentUserId();

        var response = await _workflowService.CancelAnnouncementAsync(id, userId, request.CancellationReason);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("approved")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetApprovedAnnouncements([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserId();
        var isCoordinator = User.IsInRole(RolKodu.COORDINATOR);
        var isManager = User.IsInRole(RolKodu.MANAGER);

        var response = await _approvalService.GetApprovedAnnouncementsAsync(page, pageSize, currentUserId, isCoordinator, isManager);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("rejected")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER,COORDINATOR")]
    public async Task<IActionResult> GetRejectedAnnouncements([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserId();
        var isCoordinator = User.IsInRole(RolKodu.COORDINATOR);
        var isManager = User.IsInRole(RolKodu.MANAGER);

        var response = await _approvalService.GetRejectedAnnouncementsAsync(page, pageSize, currentUserId, isCoordinator, isManager);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // REMOVED: Duplicate endpoint - AnnouncementController'da aynı endpoint var (/api/announcements/{id}/send)
    // Frontend sadece AnnouncementController'daki endpoint'i kullanıyor

    [HttpGet("{id}/can-approve")]
    public async Task<IActionResult> CanApprove(int id)
    {
        var canApprove = User.IsInRole(RolKodu.ADMIN) || User.IsInRole(RolKodu.MANAGER);

        var response = await _approvalService.CanApproveAsync(id, canApprove);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{id}/can-send")]
    public async Task<IActionResult> CanSend(int id)
    {
        var canSend = User.IsInRole(RolKodu.ADMIN) || User.IsInRole(RolKodu.MANAGER) || User.IsInRole(RolKodu.EDITOR);

        var response = await _approvalService.CanSendAsync(id, canSend);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{id}/request-changes")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> RequestChanges(int id, RequestChangesRequest request)
    {
        var kullaniciId = GetCurrentUserId();

        var response = await _workflowService.RejectAnnouncementAsync(id, kullaniciId, request.DegisiklikNotu);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            403 => Forbid(),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    #region İki Aşamalı Onay Endpoint'leri

    /// <summary>
    /// Kontrolör onayı - ILK_ONAY_BEKLIYOR → SON_ONAY_BEKLIYOR
    /// </summary>
    [HttpPost("{id}/coordinator/approve")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<IActionResult> CoordinatorApprove(int id, [FromBody] CoordinatorApproveRequest request)
    {
        var koordinatorId = GetCurrentUserId();

        var response = await _workflowService.CoordinatorApproveAsync(id, koordinatorId, request.ManagerId, request.Note);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Kontrolör reddi - ILK_ONAY_BEKLIYOR → TASLAK
    /// </summary>
    [HttpPost("{id}/coordinator/reject")]
    [Authorize(Roles = "ADMIN,COORDINATOR")]
    public async Task<IActionResult> CoordinatorReject(int id, [FromBody] RejectAnnouncementRequest request)
    {
        var koordinatorId = GetCurrentUserId();

        var response = await _workflowService.CoordinatorRejectAsync(id, koordinatorId, request.RejectionNote);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Manager onayı - SON_ONAY_BEKLIYOR → ONAYLANDI
    /// </summary>
    [HttpPost("{id}/manager/approve")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> ManagerApprove(int id, [FromBody] ApproveAnnouncementRequest request)
    {
        var managerId = GetCurrentUserId();

        var response = await _workflowService.ManagerApproveAsync(id, managerId, request.Note);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Manager reddi - SON_ONAY_BEKLIYOR → TASLAK
    /// </summary>
    [HttpPost("{id}/manager/reject")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> ManagerReject(int id, [FromBody] RejectAnnouncementRequest request)
    {
        var managerId = GetCurrentUserId();

        var response = await _workflowService.ManagerRejectAsync(id, managerId, request.RejectionNote);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Manager onayı ve direkt gönderim - SON_ONAY_BEKLIYOR → ONAYLANDI → GONDERILDI
    /// </summary>
    [HttpPost("{id}/manager/approve-and-send")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> ManagerApproveAndSend(int id, [FromBody] ApproveAnnouncementRequest request)
    {
        var managerId = GetCurrentUserId();

        var response = await _workflowService.ManagerApproveAndSendAsync(id, managerId, request.Note);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            403 => StatusCode(403, response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    #endregion İki Aşamalı Onay Endpoint'leri

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);
}