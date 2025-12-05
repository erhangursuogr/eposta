using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/announcement-recipients")]
[Authorize]
public class AnnouncementRecipientController : ControllerBase
{
    private readonly IAnnouncementRecipientService _recipientService;

    public AnnouncementRecipientController(IAnnouncementRecipientService recipientService)
    {
        _recipientService = recipientService;
    }

    [HttpGet("announcement/{announcementId}")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<IActionResult> GetRecipients(int announcementId)
    {
        var response = await _recipientService.GetRecipientsAsync(announcementId);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{announcementId}/can-modify")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<IActionResult> CanModifyRecipients(int announcementId)
    {
        var response = await _recipientService.CanModifyRecipientsAsync(announcementId);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{announcementId}/stats")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<IActionResult> GetRecipientStats(int announcementId)
    {
        var response = await _recipientService.GetRecipientStatsAsync(announcementId);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPut("{announcementId}/group/{groupId}")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<IActionResult> ReplaceGroupRecipients(int announcementId, int groupId)
    {
        var response = await _recipientService.ReplaceGroupRecipientsAsync(announcementId, groupId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{announcementId}/manual-recipient")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<IActionResult> AddManualRecipient(int announcementId, [FromBody] AddManualRecipientRequest request)
    {
        var response = await _recipientService.AddManualRecipientAsync(announcementId, request);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpDelete("{announcementId}/recipient/{recipientId}")]
    [Authorize(Roles = "ADMIN,EDITOR,MANAGER")]
    public async Task<IActionResult> RemoveRecipient(int announcementId, int recipientId)
    {
        var response = await _recipientService.RemoveRecipientAsync(announcementId, recipientId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{announcementId}/preview")]
    [Authorize(Roles = "ADMIN,MANAGER,EDITOR")]
    public async Task<IActionResult> GetRecipientPreview(int announcementId)
    {
        var response = await _recipientService.GetRecipientPreviewAsync(announcementId);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }
}