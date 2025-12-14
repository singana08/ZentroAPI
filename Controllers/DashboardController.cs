using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZentroAPI.Controllers;

/// <summary>
/// Controller for provider dashboard summary data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ITokenService tokenService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Get provider dashboard summary data
    /// </summary>
    /// <returns>Provider dashboard summary</returns>
    [HttpGet("provider")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderDashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProviderDashboard()
    {
        try
        {
            // Extract provider ID from token
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation("Getting dashboard data for provider {ProviderId}", profileId.Value);

            var (success, message, data) = await _dashboardService.GetProviderDashboardAsync(profileId.Value);

            if (!success)
            {
                if (message.Contains("not found"))
                {
                    return NotFound(new ErrorResponse { Message = message });
                }
                return BadRequest(new ErrorResponse { Message = message });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider dashboard");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get requester dashboard summary data
    /// </summary>
    /// <returns>Requester dashboard summary</returns>
    [HttpGet("requester")]
    [Authorize]
    [ProducesResponseType(typeof(RequesterDashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRequesterDashboard()
    {
        try
        {
            // Extract requester ID from token
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation("Getting dashboard data for requester {RequesterId}", profileId.Value);

            var (success, message, data) = await _dashboardService.GetRequesterDashboardAsync(profileId.Value);

            if (!success)
            {
                if (message.Contains("not found"))
                {
                    return NotFound(new ErrorResponse { Message = message });
                }
                return BadRequest(new ErrorResponse { Message = message });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving requester dashboard");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}
