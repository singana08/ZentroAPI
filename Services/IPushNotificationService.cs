using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface IPushNotificationService
{
    Task<(bool Success, string Message)> RegisterPushTokenAsync(Guid userId, RegisterPushTokenRequest request);
    Task<(bool Success, string Message)> UpdateNotificationPreferencesAsync(Guid userId, NotificationPreferencesRequest request);
    Task<(bool Success, string Message, NotificationPreferencesResponse? Data)> GetNotificationPreferencesAsync(Guid userId);
    Task<(bool Success, string Message)> SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, object>? data = null, string priority = "normal");
    Task<(bool Success, string Message)> SendBatchNotificationsAsync(List<Guid> userIds, string title, string body, Dictionary<string, object>? data = null);
    
    // Event-based notifications
    Task NotifyNewServiceRequestAsync(Guid requestId, List<Guid> providerIds);
    Task NotifyQuoteSubmittedAsync(Guid requestId, Guid requesterId);
    Task NotifyQuoteResponseAsync(Guid quoteId, Guid providerId, bool isAccepted);
    Task NotifyStatusChangeAsync(Guid requestId, string newStatus);
    Task NotifyNewMessageAsync(Guid conversationId, Guid recipientId, string senderName);
    Task NotifyPaymentReceivedAsync(Guid paymentId, Guid providerId, decimal amount);
}