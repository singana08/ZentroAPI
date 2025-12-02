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
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
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
        var profileId = User.FindFirst("profile_id")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }
        
        if (string.IsNullOrEmpty(role))
        {
            return Unauthorized(new ErrorResponse { Message = "Role not found in token" });
        }

        var (success, message) = await _notificationService.RegisterPushTokenAsync(profileGuid, request.PushToken, role);
        
        if (!success)
            return BadRequest(new ErrorResponse { Message = message });

        return Ok(new { Success = true, Message = message });
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
}
