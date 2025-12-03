namespace ZentroAPI.Services;

public interface IRealTimeNotificationService
{
    Task SendNotificationToUserAsync(Guid userId, string title, string message, string type = "info");
    Task SendNotificationToGroupAsync(string groupName, string title, string message, string type = "info");
    Task AddUserToGroupAsync(string connectionId, string groupName);
    Task RemoveUserFromGroupAsync(string connectionId, string groupName);
}