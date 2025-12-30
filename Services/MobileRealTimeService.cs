using Microsoft.AspNetCore.SignalR;
using ZentroAPI.Hubs;

namespace ZentroAPI.Services;

public interface IMobileRealTimeService
{
    Task SendServiceRequestUpdateAsync(Guid requestId, string status, object data);
    Task SendQuoteUpdateAsync(Guid quoteId, string status, object data);
    Task SendMessageAsync(Guid fromUserId, Guid toUserId, string message);
    Task SendTypingIndicatorAsync(Guid fromUserId, Guid toUserId, bool isTyping);
    Task SendPresenceUpdateAsync(Guid userId, bool isOnline);
    Task SendLocationBasedNotificationAsync(double latitude, double longitude, double radiusKm, string message);
}

public class MobileRealTimeService : IMobileRealTimeService
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly ILogger<MobileRealTimeService> _logger;

    public MobileRealTimeService(
        IHubContext<NotificationHub> notificationHub,
        IHubContext<ChatHub> chatHub,
        ILogger<MobileRealTimeService> logger)
    {
        _notificationHub = notificationHub;
        _chatHub = chatHub;
        _logger = logger;
    }

    public async Task SendServiceRequestUpdateAsync(Guid requestId, string status, object data)
    {
        await _notificationHub.Clients.Group($"request_{requestId}")
            .SendAsync("ServiceRequestUpdate", new
            {
                requestId,
                status,
                data,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task SendQuoteUpdateAsync(Guid quoteId, string status, object data)
    {
        await _notificationHub.Clients.All
            .SendAsync("QuoteUpdate", new
            {
                quoteId,
                status,
                data,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task SendMessageAsync(Guid fromUserId, Guid toUserId, string message)
    {
        await _chatHub.Clients.Group($"user_{toUserId}")
            .SendAsync("ReceiveMessage", new
            {
                fromUserId,
                message,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task SendTypingIndicatorAsync(Guid fromUserId, Guid toUserId, bool isTyping)
    {
        await _chatHub.Clients.Group($"user_{toUserId}")
            .SendAsync("TypingIndicator", new
            {
                fromUserId,
                isTyping,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task SendPresenceUpdateAsync(Guid userId, bool isOnline)
    {
        await _notificationHub.Clients.All
            .SendAsync("PresenceUpdate", new
            {
                userId,
                isOnline,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task SendLocationBasedNotificationAsync(double latitude, double longitude, double radiusKm, string message)
    {
        // This would require storing user locations and calculating proximity
        // For now, send to all users - implement geofencing logic as needed
        await _notificationHub.Clients.All
            .SendAsync("LocationNotification", new
            {
                latitude,
                longitude,
                radiusKm,
                message,
                timestamp = DateTime.UtcNow
            });
    }
}