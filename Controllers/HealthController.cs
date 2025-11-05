using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaluluAPI.Controllers;

/// <summary>
/// Health check endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
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
    public IActionResult Status()
    {
        return Ok(new
        {
            status = "healthy",
            service = "HaluluAPI",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    }
}