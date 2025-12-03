using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentroAPI.Services;

namespace ZentroAPI.Controllers;

/// <summary>
/// Health check endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }
    /// <summary>
    /// Test endpoint to verify API is accessible
    /// </summary>
    /// <returns>Success message</returns>
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new 
        { 
            success = true, 
            message = "API is accessible and working correctly",
            timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check
    /// </summary>
    /// <returns>Health details</returns>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var systemHealth = await _healthCheckService.GetSystemHealthAsync();
        var statusCode = systemHealth.Status == "Healthy" ? 200 : 503;
        
        return StatusCode(statusCode, new
        {
            service = "ZentroAPI",
            version = "1.0.0",
            timestamp = systemHealth.Timestamp,
            status = systemHealth.Status,
            uptime = systemHealth.Uptime.ToString(@"dd\.hh\:mm\:ss"),
            memoryUsage = $"{systemHealth.MemoryUsage / 1024 / 1024} MB",
            checks = systemHealth.Checks
        });
    }

    /// <summary>
    /// Database connectivity check
    /// </summary>
    [HttpGet("database")]
    public async Task<IActionResult> DatabaseHealth()
    {
        var result = await _healthCheckService.CheckDatabaseAsync();
        var statusCode = result.IsHealthy ? 200 : 503;
        return StatusCode(statusCode, result);
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult Metrics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            memoryUsage = new
            {
                workingSet = $"{process.WorkingSet64 / 1024 / 1024} MB",
                gcMemory = $"{GC.GetTotalMemory(false) / 1024 / 1024} MB"
            },
            threads = process.Threads.Count
        });
    }
}
