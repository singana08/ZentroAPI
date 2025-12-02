using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZentroAPI.Controllers;

/// <summary>
/// Controller for managing reviews and ratings
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(
        IReviewService reviewService,
        ITokenService tokenService,
        ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a review for a completed service
    /// Only requesters can submit reviews
    /// </summary>
    /// <param name="request">Review details</param>
    /// <returns>Created review</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto request)
    {
        try
        {
            // Extract customer ID from token
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation("Creating review for service request {ServiceRequestId} by customer {CustomerId}", 
                request.ServiceRequestId, profileId.Value);

            var (success, message, data) = await _reviewService.CreateReviewAsync(profileId.Value, request);

            if (!success)
            {
                return BadRequest(new ErrorResponse { Message = message });
            }

            return CreatedAtAction(nameof(GetProviderRatings), new { }, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get provider ratings and reviews data
    /// Providers can view their own ratings
    /// </summary>
    /// <returns>Provider ratings dashboard data</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ProviderRatingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProviderRatings()
    {
        try
        {
            // Extract provider ID from token
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation("Getting ratings for provider {ProviderId}", profileId.Value);

            var (success, message, data) = await _reviewService.GetProviderRatingsAsync(profileId.Value);

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
            _logger.LogError(ex, "Error retrieving provider ratings");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}
