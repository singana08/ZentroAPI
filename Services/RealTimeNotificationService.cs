using Microsoft.AspNetCore.SignalR;
using ZentroAPI.Hubs;

namespace ZentroAPI.Services;

public class RealTimeNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<RealTimeNotificationService> _logger;

    public RealTimeNotificationService(IHubContext<ChatHub> hubContext, ILogger<RealTimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(Guid userId, string title, string message, string type = "info")
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    type,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Real-time notification sent to user {UserId}: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to user {UserId}", userId);
        }
    }

    public async Task SendNotificationToGroupAsync(string groupName, string title, string message, string type = "info")
    {
        try
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    type,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Real-time notification sent to group {GroupName}: {Title}", groupName, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to group {GroupName}", groupName);
        }
    }

    public async Task AddUserToGroupAsync(string connectionId, string groupName)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
    }

    public async Task RemoveUserFromGroupAsync(string connectionId, string groupName)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
    }
}