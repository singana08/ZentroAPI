using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentroAPI.DTOs;
using ZentroAPI.Services;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PushNotificationController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        IPushNotificationService pushNotificationService,
        ITokenService tokenService,
        ILogger<PushNotificationController> logger)
    {
        _pushNotificationService = pushNotificationService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Register push token for the authenticated user
    /// </summary>
    [HttpPost("register-token")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPushToken([FromBody] RegisterPushTokenRequest request)
    {
        try
        {
            var (_, userId, _) = _tokenService.ExtractTokenInfo(User);
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "User authentication failed" });
            }

            var (success, message) = await _pushNotificationService.RegisterPushTokenAsync(userId.Value, request);

            if (!success)
            {
                return BadRequest(new ErrorResponse { Message = message });
            }

            return Ok(new SuccessResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering push token");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update notification preferences for the authenticated user
    /// </summary>
    [HttpPut("preferences")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferencesRequest request)
    {
        try
        {
            var (_, userId, _) = _tokenService.ExtractTokenInfo(User);
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "User authentication failed" });
            }

            var (success, message) = await _pushNotificationService.UpdateNotificationPreferencesAsync(userId.Value, request);

            if (!success)
            {
                return BadRequest(new ErrorResponse { Message = message });
            }

            return Ok(new SuccessResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification preferences for the authenticated user
    /// </summary>
    [HttpGet("preferences")]
    [Authorize]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        try
        {
            var (_, userId, _) = _tokenService.ExtractTokenInfo(User);
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "User authentication failed" });
            }

            var (success, message, data) = await _pushNotificationService.GetNotificationPreferencesAsync(userId.Value);

            if (!success)
            {
                return BadRequest(new ErrorResponse { Message = message });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Send push notification to specific user (Admin only)
    /// </summary>
    [HttpPost("send-push")]
    [Authorize] // TODO: Add admin role check
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendPushNotification([FromBody] SendPushNotificationRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return BadRequest(new ErrorResponse { Message = "Invalid user ID" });
            }

            var (success, message) = await _pushNotificationService.SendPushNotificationAsync(
                userId, 
                request.Title, 
                request.Body, 
                request.Data, 
                request.Priority
            );

            if (!success)
            {
                return BadRequest(new ErrorResponse { Message = message });
            }

            return Ok(new SuccessResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}