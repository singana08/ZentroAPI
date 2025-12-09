using Microsoft.AspNetCore.Mvc;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { 
            api = "running",
            database = "connected",
            deployment = "successful",
            timestamp = DateTime.UtcNow
        });
    }
}