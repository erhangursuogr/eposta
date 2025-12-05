using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/email-groups")]
[Authorize]
public class EmailGroupController : ControllerBase
{
    private readonly IEmailGroupService _emailGroupService;    

    public EmailGroupController(IEmailGroupService emailGroupService)
    {
        _emailGroupService = emailGroupService;       
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        var userRole = GetUserRole();
        var response = await _emailGroupService.GetGroupsAsync(page, pageSize, searchTerm, userRole);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        var response = await _emailGroupService.GetGroupByIdAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateGroup(CreateEmailGroupDto request)
    {       
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.CreateGroupAsync(request, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            409 => Conflict(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateGroup(int id, UpdateEmailGroupDto request)
    {
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.UpdateGroupAsync(id, request, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            409 => Conflict(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.DeleteGroupAsync(id, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetGroupMembers(int id)
    {
        var response = await _emailGroupService.GetGroupMembersAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{id}/members")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddMember(int id, AddGroupMemberRequest request)
    {
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.AddMemberAsync(id, request, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            409 => Conflict(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPut("{groupId}/members/{memberId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateMember(int groupId, int memberId, UpdateGroupMemberRequest request)
    {
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.UpdateMemberAsync(groupId, memberId, request, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpDelete("{groupId}/members/{memberId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RemoveMember(int groupId, int memberId)
    {
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.RemoveMemberAsync(groupId, memberId, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("{id}/import-members")]
    [Authorize(Roles = "ADMIN")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> ImportMembers(int id, IFormFile file)
    {
        var kullaniciId = GetCurrentUserId();
        var response = await _emailGroupService.ImportMembersFromFileAsync(id, file, kullaniciId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("{id}/emails")]
    public async Task<IActionResult> GetGroupEmails(int id)
    {
        var response = await _emailGroupService.GetGroupEmailsAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpPost("preview-dynamic")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> PreviewDynamicGroup([FromBody] PreviewDynamicGroupRequest request)
    {
        var response = await _emailGroupService.PreviewDynamicGroupAsync(request.ViewAdi, request.FilterKosulu);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "VIEWER";
    }
    
}