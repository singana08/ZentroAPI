using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface INotificationService
{
    Task<(bool Success, string Message)> RegisterPushTokenAsync(Guid profileId, string pushToken, string role);
    Task<(bool Success, string Message, int NotificationsSent)> NotifyProvidersOfNewRequestAsync(NotifyProvidersRequest request);
    Task<NotificationsListResponse> GetProfileNotificationsAsync(Guid profileId, int page, int pageSize, bool unreadOnly);
    Task<(bool Success, string Message)> MarkNotificationAsReadAsync(int notificationId, Guid profileId);
    Task<(bool Success, int UpdatedCount, string Message)> MarkAllNotificationsAsReadAsync(Guid profileId);
    Task<int> GetUnreadCountAsync(Guid profileId);
    Task CreateNotificationAsync(Guid profileId, string title, string body, string type, Guid? relatedEntityId = null, object? data = null);
}
