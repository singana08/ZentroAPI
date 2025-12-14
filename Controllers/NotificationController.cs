using ZentroAPI.DTOs;
using ZentroAPI.Services;
using ZentroAPI.Data;
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
    private readonly ApplicationDbContext _context;

    public NotificationController(
        INotificationService notificationService, 
        IPushNotificationService pushNotificationService,
        ITokenService tokenService,
        ILogger<NotificationController> logger,
        ApplicationDbContext context)
    {
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
        _tokenService = tokenService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Check if push token registration is needed
    /// </summary>
    [HttpGet("token-status")]
    [Authorize]
    [ProducesResponseType(typeof(TokenStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTokenStatus([FromQuery] string? currentToken = null)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Ok(new TokenStatusResponse { NeedsRegistration = true, Message = "Profile not found" });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
            {
                return Ok(new TokenStatusResponse { NeedsRegistration = true, Message = "Role not found" });
            }

            string? storedToken = null;
            if (role.ToLower() == "provider")
            {
                var provider = await _context.Providers.FindAsync(profileId.Value);
                storedToken = provider?.PushToken;
            }
            else if (role.ToLower() == "requester")
            {
                var requester = await _context.Requesters.FindAsync(profileId.Value);
                storedToken = requester?.PushToken;
            }

            var needsRegistration = string.IsNullOrEmpty(storedToken) || 
                                  (!string.IsNullOrEmpty(currentToken) && storedToken != currentToken);

            return Ok(new TokenStatusResponse 
            { 
                NeedsRegistration = needsRegistration,
                Message = needsRegistration ? "Token registration required" : "Token is current"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token status");
            return Ok(new TokenStatusResponse { NeedsRegistration = true, Message = "Error checking status" });
        }
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
    /// Debug: Check providers with notifications enabled
    /// </summary>
    [HttpGet("debug/providers")]
    [Authorize]
    public async Task<IActionResult> DebugProviders()
    {
        try
        {
            var providers = await _context.Providers
                .Select(p => new { 
                    p.Id, 
                    p.IsActive, 
                    p.NotificationsEnabled, 
                    HasPushToken = !string.IsNullOrEmpty(p.PushToken),
                    PushTokenPreview = p.PushToken != null ? p.PushToken.Substring(0, Math.Min(20, p.PushToken.Length)) + "..." : null
                })
                .ToListAsync();
                
            var activeWithNotifications = providers.Count(p => p.IsActive && p.NotificationsEnabled);
            var withPushTokens = providers.Count(p => p.HasPushToken);
            
            return Ok(new { 
                TotalProviders = providers.Count,
                ActiveWithNotifications = activeWithNotifications,
                WithPushTokens = withPushTokens,
                Providers = providers 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debugging providers");
            return StatusCode(500, new { Error = ex.Message });
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

    /// <summary>
    /// Test provider query directly
    /// </summary>
    [HttpGet("test-provider-query")]
    [Authorize]
    public async Task<IActionResult> TestProviderQuery()
    {
        try
        {
            var allProviders = await _context.Providers.CountAsync();
            var activeProviders = await _context.Providers.CountAsync(p => p.IsActive);
            var notificationProviders = await _context.Providers.CountAsync(p => p.NotificationsEnabled);
            var bothProviders = await _context.Providers.CountAsync(p => p.IsActive && p.NotificationsEnabled);
            
            var providerDetails = await _context.Providers
                .Select(p => new { p.Id, p.IsActive, p.NotificationsEnabled, HasToken = !string.IsNullOrEmpty(p.PushToken) })
                .ToListAsync();

            return Ok(new { 
                Total = allProviders,
                Active = activeProviders,
                NotificationsEnabled = notificationProviders,
                ActiveAndNotifications = bothProviders,
                Details = providerDetails
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Manual test: Create fake service request and test notifications
    /// </summary>
    [HttpPost("test-manual")]
    [Authorize]
    public async Task<IActionResult> TestManualNotification()
    {
        try
        {
            // Create a test service request
            var testRequest = new ServiceRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = Guid.NewGuid(),
                MainCategory = "Test Category",
                SubCategory = "Test Service",
                Location = "Test Location",
                Status = ServiceRequestStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.ServiceRequests.Add(testRequest);
            await _context.SaveChangesAsync();

            // Test notification
            await _notificationService.NotifyProvidersOfNewServiceRequestAsync(testRequest.Id);

            return Ok(new { Success = true, Message = "Test notification sent", ServiceRequestId = testRequest.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual test");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
