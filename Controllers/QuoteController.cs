using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaluluAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class QuoteController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<QuoteController> _logger;

    public QuoteController(IQuoteService quoteService, ITokenService tokenService, ILogger<QuoteController> logger)
    {
        _quoteService = quoteService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a quote for a service request
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(QuoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _quoteService.CreateQuoteAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get quotes for a service request
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(QuotesListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetQuotes([FromQuery] Guid requestId)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _quoteService.GetQuotesForRequestAsync(requestId, profileId.Value);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quotes");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}