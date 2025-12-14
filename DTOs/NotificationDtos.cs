using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.DTOs;

public class RegisterPushTokenRequest
{
    [Required]
    public string PushToken { get; set; } = string.Empty;
    
    [Required]
    public string DeviceType { get; set; } = string.Empty; // "ios" or "android"
    
    public string? DeviceId { get; set; }
    
    public string? AppVersion { get; set; }
}

public class NotifyProvidersRequest
{
    [Required]
    public Guid ServiceRequestId { get; set; }
    
    [Required]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    public string SubCategory { get; set; } = string.Empty;
    
    [Required]
    public string Location { get; set; } = string.Empty;
}

public class NotificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int NotificationsSent { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationsListResponse
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class UnreadCountResponse
{
    public int UnreadCount { get; set; }
}

public class MarkAllReadResponse
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PushNotificationPayload
{
    public string To { get; set; } = string.Empty;
    public string Sound { get; set; } = "default";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}

public class NotificationPreferencesRequest
{
    public bool EnablePushNotifications { get; set; }
    public bool NewRequests { get; set; }
    public bool QuoteResponses { get; set; }
    public bool StatusUpdates { get; set; }
    public bool Messages { get; set; }
    public bool Reminders { get; set; }
}

public class NotificationPreferencesResponse
{
    public bool EnablePushNotifications { get; set; }
    public bool NewRequests { get; set; }
    public bool QuoteResponses { get; set; }
    public bool StatusUpdates { get; set; }
    public bool Messages { get; set; }
    public bool Reminders { get; set; }
}

public class SendPushNotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public string Priority { get; set; } = "normal"; // "high" or "normal"
    public string Sound { get; set; } = "default";
}

public class SuccessResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}

public class ExpoMessage
{
    public string To { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public string Priority { get; set; } = "normal";
    public string Sound { get; set; } = "default";
}
