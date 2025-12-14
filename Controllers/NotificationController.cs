using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService, 
        IPushNotificationService pushNotificationService,
        ITokenService tokenService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Register push token for current profile
    /// </summary>
    [HttpPost("register-token")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPushToken([FromBody] RegisterPushTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Push token registration attempt: {PushToken}, Device: {DeviceType}, ID: {DeviceId}", 
                request.PushToken, request.DeviceType, request.DeviceId);

            // Extract profile ID from token for new push notification system
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            _logger.LogInformation("Extracted profileId from token: {ProfileId}", profileId);
            
            if (!profileId.HasValue)
            {
                _logger.LogWarning("Profile ID not found in token");
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            // Register with new push notification service (if tables exist)
            try
            {
                _logger.LogInformation("Attempting to register with new push notification service for profile {ProfileId}", profileId.Value);
                var (newSuccess, newMessage) = await _pushNotificationService.RegisterPushTokenAsync(profileId.Value, request);
                _logger.LogInformation("New push service result: Success={Success}, Message={Message}", newSuccess, newMessage);
                
                if (newSuccess)
                {
                    return Ok(new { Success = true, Message = "Push token registered successfully" });
                }
                else
                {
                    _logger.LogWarning("New push service failed: {Message}", newMessage);
                    return BadRequest(new ErrorResponse { Message = newMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "New push notification service failed, using legacy method");
            }

            // Fallback to old method for backward compatibility
            var legacyProfileId = User.FindFirst("profile_id")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("Fallback to legacy method: ProfileId={ProfileId}, Role={Role}", legacyProfileId, role);
            
            if (string.IsNullOrEmpty(legacyProfileId) || !Guid.TryParse(legacyProfileId, out var profileGuid))
            {
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
            }
            
            if (string.IsNullOrEmpty(role))
            {
                return Unauthorized(new ErrorResponse { Message = "Role not found in token" });
            }

            var (legacySuccess, legacyMessage) = await _notificationService.RegisterPushTokenAsync(profileGuid, request.PushToken, role);
            _logger.LogInformation("Legacy service result: Success={Success}, Message={Message}", legacySuccess, legacyMessage);
            
            if (!legacySuccess)
                return BadRequest(new ErrorResponse { Message = legacyMessage });

            return Ok(new { Success = true, Message = legacyMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering push token");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get profile notifications with pagination
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(NotificationsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool unreadOnly = false)
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        var result = await _notificationService.GetProfileNotificationsAsync(profileGuid, page, pageSize, unreadOnly);
        return Ok(result);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        var (success, message) = await _notificationService.MarkNotificationAsReadAsync(id, profileGuid);
        
        if (!success)
            return NotFound(new ErrorResponse { Message = message });

        return Ok(new { Success = true, Message = message });
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("mark-all-read")]
    [Authorize]
    [ProducesResponseType(typeof(MarkAllReadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        var (success, updatedCount, message) = await _notificationService.MarkAllNotificationsAsReadAsync(profileGuid);
        
        return Ok(new MarkAllReadResponse
        {
            Success = success,
            UpdatedCount = updatedCount,
            Message = message
        });
    }

    /// <summary>
    /// Get unread notifications count
    /// </summary>
    [HttpGet("unread-count")]
    [Authorize]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        var unreadCount = await _notificationService.GetUnreadCountAsync(profileGuid);
        
        return Ok(new UnreadCountResponse { UnreadCount = unreadCount });
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
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            var (success, message) = await _pushNotificationService.UpdateNotificationPreferencesAsync(profileId.Value, request);

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
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            var (success, message, data) = await _pushNotificationService.GetNotificationPreferencesAsync(profileId.Value);

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
    /// Send push notification to specific profile (Admin only)
    /// </summary>
    [HttpPost("send-push")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendPushNotification([FromBody] SendPushNotificationRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.ProfileId, out var profileId))
            {
                return BadRequest(new ErrorResponse { Message = "Invalid profile ID" });
            }

            var (success, message) = await _pushNotificationService.SendPushNotificationAsync(
                profileId, 
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

    /// <summary>
    /// Test notification scenarios (Development only)
    /// </summary>
    [HttpPost("test/{scenario}")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestNotificationScenario(string scenario, [FromBody] TestNotificationRequest request)
    {
        try
        {
            switch (scenario.ToLower())
            {
                case "new-request":
                    await _notificationService.NotifyProvidersOfNewServiceRequestAsync(request.ServiceRequestId);
                    return Ok(new SuccessResponse { Message = "New request notifications sent" });
                    
                case "request-update":
                    await _notificationService.NotifyProviderOfRequestUpdateAsync(request.ServiceRequestId, "edit");
                    return Ok(new SuccessResponse { Message = "Request update notification sent" });
                    
                case "request-cancel":
                    await _notificationService.NotifyProviderOfRequestUpdateAsync(request.ServiceRequestId, "cancel");
                    return Ok(new SuccessResponse { Message = "Request cancellation notification sent" });
                    
                case "quote-accept":
                    await _notificationService.NotifyOfQuoteAcceptanceAsync(request.QuoteId, request.AcceptingProfileId, request.IsRequester);
                    return Ok(new SuccessResponse { Message = "Quote acceptance notification sent" });
                    
                default:
                    return BadRequest(new ErrorResponse { Message = "Invalid scenario. Use: new-request, request-update, request-cancel, quote-accept" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing notification scenario: {Scenario}", scenario);
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}
