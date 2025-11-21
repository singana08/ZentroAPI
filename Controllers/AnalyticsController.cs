using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaluluAPI.Controllers;

/// <summary>
/// Controller for provider analytics and performance metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ITokenService tokenService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Get provider performance analytics and completion rate data
    /// </summary>
    /// <returns>Provider analytics dashboard data</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ProviderAnalyticsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProviderAnalytics()
    {
        try
        {
            // Extract provider ID from token
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation("Getting analytics for provider {ProviderId}", profileId.Value);

            var (success, message, data) = await _analyticsService.GetProviderAnalyticsAsync(profileId.Value);

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
            _logger.LogError(ex, "Error retrieving provider analytics");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}