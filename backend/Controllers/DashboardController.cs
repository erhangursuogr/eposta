using DeuEposta.Models.Enums;
using DeuEposta.Services;
using DeuEposta.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats(
        [FromQuery] bool onlyMine = false)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole(RolKodu.ADMIN);
        var isManager = User.IsInRole(RolKodu.MANAGER);
        var isCoordinator = User.IsInRole(RolKodu.COORDINATOR);
        var kullaniciId = onlyMine ? (int?)currentUserId : null;

        var response = await _dashboardService.GetDashboardStatsAsync(kullaniciId, currentUserId, isAdmin, isManager, isCoordinator);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("recent-activities")]
    public async Task<IActionResult> GetRecentActivities(
        [FromQuery] int count = 10)
    {
        var response = await _dashboardService.GetRecentActivitiesAsync(count);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("announcement-chart")]
    public async Task<IActionResult> GetAnnouncementChartData(
        [FromQuery] int days = 30)
    {
        var response = await _dashboardService.GetAnnouncementChartDataAsync(days);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("group-stats")]
    public async Task<IActionResult> GetGroupStats()
    {
        var response = await _dashboardService.GetGroupStatsAsync();

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("system-health")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var response = await _dashboardService.GetSystemHealthAsync();

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    [HttpGet("top-users")]
    public async Task<IActionResult> GetTopUsers(
        [FromQuery] int count = 5)
    {
        var response = await _dashboardService.GetTopUsersAsync(count);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    private int GetCurrentUserId() => HttpContextHelper.GetCurrentUserId(User);
}