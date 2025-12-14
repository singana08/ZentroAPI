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

    public async Task NotifyProvidersOfNewServiceRequestAsync(Guid serviceRequestId)
    {
        try
        {
            _logger.LogInformation($"Starting notification process for service request {serviceRequestId}");
            
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Requester)
                .ThenInclude(r => r!.User)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null) 
            {
                _logger.LogWarning($"Service request {serviceRequestId} not found for notifications");
                return;
            }

            _logger.LogInformation($"Found service request: {serviceRequest.SubCategory} in {serviceRequest.Location}");

            var providers = await _context.Providers
                .Where(p => p.IsActive && p.NotificationsEnabled)
                .ToListAsync();
                
            _logger.LogError($"DEBUG: Found {providers.Count} active providers with notifications enabled");
            
            if (providers.Count == 0)
            {
                // Check total providers
                var totalProviders = await _context.Providers.CountAsync();
                var activeProviders = await _context.Providers.CountAsync(p => p.IsActive);
                var notificationEnabledProviders = await _context.Providers.CountAsync(p => p.NotificationsEnabled);
                
                _logger.LogError($"DEBUG: Total providers: {totalProviders}, Active: {activeProviders}, NotificationsEnabled: {notificationEnabledProviders}");
                return;
            }

            var notifications = new List<PushNotificationPayload>();

            foreach (var provider in providers)
            {
                // Create in-app notification
                await CreateNotificationAsync(
                    provider.Id,
                    "New Service Request",
                    $"{serviceRequest.SubCategory} service needed in {serviceRequest.Location}",
                    "new_service_request",
                    serviceRequestId,
                    new { serviceRequestId, category = serviceRequest.MainCategory, subCategory = serviceRequest.SubCategory }
                );

                // Add push notification if token exists
                if (!string.IsNullOrEmpty(provider.PushToken))
                {
                    notifications.Add(new PushNotificationPayload
                    {
                        To = provider.PushToken,
                        Title = "New Service Request",
                        Body = $"{serviceRequest.SubCategory} service needed in {serviceRequest.Location}",
                        Data = new
                        {
                            serviceRequestId = serviceRequestId.ToString(),
                            category = serviceRequest.MainCategory,
                            subCategory = serviceRequest.SubCategory,
                            location = serviceRequest.Location,
                            type = "new_service_request"
                        }
                    });
                }
            }

            _logger.LogInformation($"Prepared {notifications.Count} push notifications");
            
            if (notifications.Any())
            {
                var sentCount = await SendBatchNotificationsAsync(notifications);
                _logger.LogInformation($"Sent {sentCount} push notifications successfully");
            }

            _logger.LogInformation($"Completed notification process: {providers.Count} providers notified for service request {serviceRequestId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error notifying providers of new service request {serviceRequestId}");
        }
    }

    public async Task NotifyProviderOfRequestUpdateAsync(Guid serviceRequestId, string updateType)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.AssignedProvider)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest?.AssignedProvider == null || !serviceRequest.AssignedProvider.NotificationsEnabled) return;

            var title = updateType.ToLower() switch
            {
                "edit" => "Service Request Updated",
                "cancel" => "Service Request Cancelled",
                _ => "Service Request Changed"
            };

            var body = updateType.ToLower() switch
            {
                "edit" => $"The {serviceRequest.SubCategory} request has been updated",
                "cancel" => $"The {serviceRequest.SubCategory} request has been cancelled",
                _ => $"The {serviceRequest.SubCategory} request has been modified"
            };

            // Create in-app notification
            await CreateNotificationAsync(
                serviceRequest.AssignedProvider.Id,
                title,
                body,
                $"request_{updateType.ToLower()}",
                serviceRequestId,
                new { serviceRequestId, updateType, category = serviceRequest.MainCategory, subCategory = serviceRequest.SubCategory }
            );

            // Send push notification if token exists
            if (!string.IsNullOrEmpty(serviceRequest.AssignedProvider.PushToken))
            {
                var pushNotification = new PushNotificationPayload
                {
                    To = serviceRequest.AssignedProvider.PushToken,
                    Title = title,
                    Body = body,
                    Data = new
                    {
                        serviceRequestId = serviceRequestId.ToString(),
                        updateType,
                        category = serviceRequest.MainCategory,
                        subCategory = serviceRequest.SubCategory,
                        type = $"request_{updateType.ToLower()}"
                    }
                };

                await SendBatchNotificationsAsync(new List<PushNotificationPayload> { pushNotification });
            }

            _logger.LogInformation($"Notified provider {serviceRequest.AssignedProvider.Id} of request {updateType} for {serviceRequestId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error notifying provider of request {updateType} for {serviceRequestId}");
        }
    }

    public async Task NotifyOfQuoteAcceptanceAsync(Guid quoteId, Guid acceptingProfileId, bool isRequester)
    {
        try
        {
            var quote = await _context.Quotes
                .Include(q => q.ServiceRequest)
                .Include(q => q.Provider)
                .ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote?.ServiceRequest == null) return;

            var targetProfileId = isRequester ? quote.ProviderId : quote.ServiceRequest.RequesterId;
            var acceptingUserName = isRequester ? "Customer" : quote.Provider?.User?.FullName ?? "Provider";

            var title = isRequester ? "Quote Accepted!" : "Quote Response";
            var body = isRequester 
                ? $"Your quote of ₹{quote.Price:F2} for {quote.ServiceRequest.SubCategory} has been accepted!"
                : $"{acceptingUserName} accepted the quote for {quote.ServiceRequest.SubCategory}";

            // Create in-app notification
            await CreateNotificationAsync(
                targetProfileId,
                title,
                body,
                "quote_accepted",
                quote.ServiceRequest.Id,
                new { quoteId, serviceRequestId = quote.ServiceRequest.Id, price = quote.Price, acceptedBy = acceptingProfileId }
            );

            // Send push notification
            var targetProvider = isRequester ? null : await _context.Providers.FindAsync(targetProfileId);
            var targetRequester = isRequester ? await _context.Requesters.FindAsync(targetProfileId) : null;
            
            // Check if notifications are enabled for the target user
            var notificationsEnabled = targetProvider?.NotificationsEnabled ?? targetRequester?.NotificationsEnabled ?? false;
            if (!notificationsEnabled) return;
            
            var pushToken = targetProvider?.PushToken ?? targetRequester?.PushToken;
            
            if (!string.IsNullOrEmpty(pushToken))
            {
                var pushNotification = new PushNotificationPayload
                {
                    To = pushToken,
                    Title = title,
                    Body = body,
                    Data = new
                    {
                        quoteId = quoteId.ToString(),
                        serviceRequestId = quote.ServiceRequest.Id.ToString(),
                        price = quote.Price,
                        acceptedBy = acceptingProfileId.ToString(),
                        type = "quote_accepted"
                    }
                };

                await SendBatchNotificationsAsync(new List<PushNotificationPayload> { pushNotification });
            }

            _logger.LogInformation($"Notified {targetProfileId} of quote acceptance for quote {quoteId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error notifying of quote acceptance for quote {quoteId}");
        }
    }
}
