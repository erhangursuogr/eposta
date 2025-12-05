using DeuEposta.Models.DTOs;
using DeuEposta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DeuEposta.Controllers;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = "ADMIN")]
[EnableRateLimiting("Api")]
public class LogController : ControllerBase
{
    private readonly ILogService _logService;

    public LogController(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// Login loglarını filtreli olarak getirir
    /// </summary>
    [HttpGet("login")]
    public async Task<IActionResult> GetLoginLogs([FromQuery] LoginLogFilterRequest request)
    {
        var response = await _logService.GetLoginLogsAsync(request);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Sistem loglarını filtreli olarak getirir
    /// </summary>
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemLogs([FromQuery] SystemLogFilterRequest request)
    {
        var response = await _logService.GetSystemLogsAsync(request);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }

    /// <summary>
    /// Email gönderim loglarını filtreli olarak getirir
    /// </summary>
    [HttpGet("email")]
    public async Task<IActionResult> GetEmailLogs([FromQuery] EmailLogFilterRequest request)
    {
        var response = await _logService.GetEmailLogsAsync(request);

        return response.StatusCode switch
        {
            500 => StatusCode(500, response),
            _ => Ok(response)
        };
    }
}