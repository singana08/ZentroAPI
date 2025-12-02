using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ZentroAPI.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly HttpClient _httpClient;
    private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger, HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<(bool Success, string Message)> RegisterPushTokenAsync(Guid profileId, string pushToken, string role)
    {
        try
        {
            // Validate token format
            if (!pushToken.StartsWith("ExponentPushToken["))
            {
                return (false, "Invalid push token format");
            }

            if (role.ToLower() == "requester")
            {
                var requester = await _context.Requesters.FindAsync(profileId);
                if (requester == null)
                {
                    return (false, "Requester profile not found");
                }

                requester.PushToken = pushToken;
                requester.UpdatedAt = DateTime.UtcNow;
            }
            else if (role.ToLower() == "provider")
            {
                var provider = await _context.Providers.FindAsync(profileId);
                if (provider == null)
                {
                    return (false, "Provider profile not found");
                }

                provider.PushToken = pushToken;
                provider.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                return (false, "Invalid role specified");
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Push token registered for {role} profile {profileId}");
            return (true, "Push token registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering push token for {role} profile {profileId}");
            return (false, "Failed to register push token");
        }
    }

    public async Task<(bool Success, string Message, int NotificationsSent)> NotifyProvidersOfNewRequestAsync(NotifyProvidersRequest request)
    {
        try
        {
            // Get all providers with push tokens and notifications enabled
            var providerTokens = await _context.Providers
                .Where(p => p.IsActive && 
                           !string.IsNullOrEmpty(p.PushToken) && 
                           p.NotificationsEnabled)
                .Select(p => p.PushToken!)
                .ToListAsync();

            if (!providerTokens.Any())
            {
                return (true, "No providers to notify", 0);
            }

            var notifications = providerTokens.Select(token => new PushNotificationPayload
            {
                To = token,
                Title = "New Service Request",
                Body = $"{request.SubCategory} service needed in {request.Location}",
                Data = new
                {
                    serviceRequestId = request.ServiceRequestId.ToString(),
                    category = request.Category,
                    subCategory = request.SubCategory,
                    location = request.Location,
                    type = "new_service_request"
                }
            }).ToList();

            var sentCount = await SendBatchNotificationsAsync(notifications);
            
            _logger.LogInformation($"Sent {sentCount} notifications for service request {request.ServiceRequestId}");
            return (true, "Notifications sent to providers", sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notifications for service request {request.ServiceRequestId}");
            return (false, "Failed to send notifications", 0);
        }
    }

    private async Task<int> SendBatchNotificationsAsync(List<PushNotificationPayload> notifications)
    {
        var sentCount = 0;
        
        try
        {
            var json = JsonSerializer.Serialize(notifications);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(ExpoApiUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Expo API response: {responseContent}");
                sentCount = notifications.Count;
            }
            else
            {
                _logger.LogWarning($"Expo API error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending batch notifications to Expo");
        }
        
        return sentCount;
    }

    public async Task<NotificationsListResponse> GetProfileNotificationsAsync(Guid profileId, int page, int pageSize, bool unreadOnly)
    {
        var query = _context.Notifications.Where(n => n.ProfileId == profileId);
        
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);
        
        var totalCount = await query.CountAsync();
        var unreadCount = await _context.Notifications.CountAsync(n => n.ProfileId == profileId && !n.IsRead);
        
        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Data = n.Data,
                IsRead = n.IsRead,
                NotificationType = n.NotificationType,
                RelatedEntityId = n.RelatedEntityId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
        
        return new NotificationsListResponse
        {
            Notifications = notifications,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<(bool Success, string Message)> MarkNotificationAsReadAsync(int notificationId, Guid profileId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ProfileId == profileId);
            
            if (notification == null)
                return (false, "Notification not found");
            
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            
            return (true, "Notification marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking notification {notificationId} as read");
            return (false, "Failed to mark notification as read");
        }
    }

    public async Task<(bool Success, int UpdatedCount, string Message)> MarkAllNotificationsAsReadAsync(Guid profileId)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.ProfileId == profileId && !n.IsRead)
                .ToListAsync();
            
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            
            await _context.SaveChangesAsync();
            
            return (true, unreadNotifications.Count, "All notifications marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking all notifications as read for profile {profileId}");
            return (false, 0, "Failed to mark notifications as read");
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid profileId)
    {
        return await _context.Notifications.CountAsync(n => n.ProfileId == profileId && !n.IsRead);
    }

    public async Task CreateNotificationAsync(Guid profileId, string title, string body, string type, Guid? relatedEntityId = null, object? data = null)
    {
        try
        {
            var notification = new Notification
            {
                ProfileId = profileId,
                Title = title,
                Body = body,
                NotificationType = type,
                RelatedEntityId = relatedEntityId,
                Data = data != null ? JsonSerializer.Serialize(data) : null
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created notification for profile {profileId}: {title}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating notification for profile {profileId}");
        }
    }
}
