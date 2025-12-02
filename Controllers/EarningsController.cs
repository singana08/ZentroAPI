using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZentroAPI.Controllers;

/// <summary>
/// Controller for provider earnings data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EarningsController : ControllerBase
{
    private readonly IEarningsService _earningsService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<EarningsController> _logger;

    public EarningsController(
        IEarningsService earningsService,
        ITokenService tokenService,
        ILogger<EarningsController> logger)
    {
        _earningsService = earningsService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Get provider earnings data with period-based filtering
    /// </summary>
    /// <param name="period">Optional period filter: today, week, month, quarter, year</param>
    /// <returns>Provider earnings data</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ProviderEarningsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProviderEarnings([FromQuery] string? period = null)
    {
        try
        {
            // Extract provider ID from token
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation("Getting earnings for provider {ProviderId}, period: {Period}", profileId.Value, period);

            var (success, message, data) = await _earningsService.GetProviderEarningsAsync(profileId.Value, period);

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
            _logger.LogError(ex, "Error retrieving provider earnings");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}
