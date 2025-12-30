using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ZentroAPI.Services;

namespace ZentroAPI.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ITokenService tokenService, ILogger<NotificationHub> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var (profileId, role, _) = _tokenService.ExtractTokenInfo(Context.User!);
        if (profileId.HasValue)
        {
            // Add to user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{profileId.Value}");
            
            // Add to role-specific group for broadcasts
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
            
            _logger.LogInformation($"User {profileId.Value} ({role}) connected to notifications");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (profileId, role, _) = _tokenService.ExtractTokenInfo(Context.User!);
        if (profileId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{profileId.Value}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{role}");
            _logger.LogInformation($"User {profileId.Value} disconnected from notifications");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Mobile-specific methods
    public async Task UpdateLocation(double latitude, double longitude)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(Context.User!);
        if (profileId.HasValue)
        {
            // Update user location for proximity-based notifications
            await Clients.Caller.SendAsync("LocationUpdated", new { latitude, longitude });
        }
    }

    public async Task MarkNotificationRead(string notificationId)
    {
        await Clients.Caller.SendAsync("NotificationMarkedRead", notificationId);
    }
}