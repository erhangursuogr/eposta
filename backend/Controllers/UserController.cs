using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    // GET: api/User
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? activeOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await _userService.GetUsersAsync(search, role, activeOnly ?? "", page, pageSize);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // GET: api/User/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetUser(int id)
    {
        var response = await _userService.GetUserByIdAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // POST: api/User
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        // Get current admin user ID from claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int createdByUserId))
        {
            return Unauthorized(new { success = false, statusCode = 401, message = "Kullanıcı kimliği doğrulanamadı" });
        }

        var response = await _userService.CreateUserAsync(request, createdByUserId);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            500 => StatusCode(500, response),
            _ => CreatedAtAction(nameof(GetUser), new { id = response.Data?.Id }, response)
        };
    }

    // PUT: api/User/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request)
    {
        var response = await _userService.UpdateUserAsync(id, request);

        return response.StatusCode switch
        {
            400 => BadRequest(response),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // DELETE: api/User/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var response = await _userService.DeleteUserAsync(id);

        return response.StatusCode switch
        {
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // GET: api/User/statistics
    [HttpGet("statistics")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetUserStatistics()
    {
        var response = await _userService.GetUserStatisticsAsync();

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    // GET: api/User/approvers
    /// <summary>
    /// Duyuru onaylayabilecek kullanıcıları getirir (MANAGER rolü)
    /// </summary>
    [HttpGet("approvers")]
    [Authorize(Roles = "ADMIN,EDITOR,COORDINATOR")]
    public async Task<IActionResult> GetApprovers()
    {
        var response = await _userService.GetApproversAsync();

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }
}