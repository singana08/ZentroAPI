using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PushNotificationService> _logger;
    private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";

    public PushNotificationService(ApplicationDbContext context, HttpClient httpClient, ILogger<PushNotificationService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> RegisterPushTokenAsync(Guid profileId, RegisterPushTokenRequest request)
    {
        try
        {
            // Get user ID from profile ID
            var userIdQuery = await _context.Requesters
                .Where(r => r.Id == profileId)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();
            
            if (userIdQuery == Guid.Empty)
            {
                userIdQuery = await _context.Providers
                    .Where(p => p.Id == profileId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
            }
            
            if (userIdQuery == Guid.Empty)
            {
                return (false, "Profile not found");
            }

            // Check if push token already exists
            var existingToken = await _context.UserPushTokens
                .FirstOrDefaultAsync(t => t.PushToken == request.PushToken);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.UserId = userIdQuery;
                existingToken.DeviceType = request.DeviceType.ToLower();
                existingToken.DeviceId = request.DeviceId;
                existingToken.AppVersion = request.AppVersion;
                existingToken.IsActive = true;
                existingToken.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Deactivate old tokens for same device
                if (!string.IsNullOrEmpty(request.DeviceId))
                {
                    var oldTokens = await _context.UserPushTokens
                        .Where(t => t.UserId == userIdQuery && t.DeviceId == request.DeviceId)
                        .ToListAsync();

                    foreach (var token in oldTokens)
                    {
                        token.IsActive = false;
                        token.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Add new token
                var pushToken = new UserPushToken
                {
                    UserId = userIdQuery,
                    PushToken = request.PushToken,
                    DeviceType = request.DeviceType.ToLower(),
                    DeviceId = request.DeviceId,
                    AppVersion = request.AppVersion,
                    IsActive = true
                };

                _context.UserPushTokens.Add(pushToken);
            }

            await _context.SaveChangesAsync();

            return (true, "Push token registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering push token for profile {ProfileId}", profileId);
            return (false, "Failed to register push token");
        }
    }

    public async Task<(bool Success, string Message)> UpdateNotificationPreferencesAsync(Guid profileId, NotificationPreferencesRequest request)
    {
        try
        {
            // Get user ID from profile ID
            var userIdQuery = await _context.Requesters
                .Where(r => r.Id == profileId)
                .Select(r => r.UserId)
                .FirstOrDefaultAsync();
            
            if (userIdQuery == Guid.Empty)
            {
                userIdQuery = await _context.Providers
                    .Where(p => p.Id == profileId)
                    .Select(p => p.UserId)
                    .FirstOrDefaultAsync();
            }
            
            if (userIdQuery == Guid.Empty)
            {
                return (false, "Profile not found");
            }

            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userIdQuery);

            if (preferences == null)
            {
                preferences = new NotificationPreferences { UserId = userIdQuery };
                _context.NotificationPreferences.Add(preferences);
            }

            preferences.EnablePushNotifications = request.EnablePushNotifications;
            preferences.NewRequests = request.NewRequests;
            preferences.QuoteResponses = request.QuoteResponses;
            preferences.StatusUpdates = request.StatusUpdates;
            preferences.Messages = request.Messages;
            preferences.Reminders = request.Reminders;
            preferences.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "Notification preferences updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for profile {ProfileId}", profileId);
            return (false, "Failed to update notification preferences");
        }
    }

    public async Task<(bool Success, string Message, NotificationPreferencesResponse? Data)> GetNotificationPreferencesAsync(Guid profileId)
    {
        try
        {
            // Get user ID from profile ID
            var userIdQuery = await _context.Requesters
                .Where(r => r.Id == profileId)
                .Select(r => r.UserId)
                .FirstOrDefaultAsync();
            
            if (userIdQuery == Guid.Empty)
            {
                userIdQuery = await _context.Providers
                    .Where(p => p.Id == profileId)
                    .Select(p => p.UserId)
                    .FirstOrDefaultAsync();
            }
            
            if (userIdQuery == Guid.Empty)
            {
                return (false, "Profile not found", null);
            }

            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userIdQuery);

            if (preferences == null)
            {
                // Create default preferences
                preferences = new NotificationPreferences { UserId = userIdQuery };
                _context.NotificationPreferences.Add(preferences);
                await _context.SaveChangesAsync();
            }

            var response = new NotificationPreferencesResponse
            {
                EnablePushNotifications = preferences.EnablePushNotifications,
                NewRequests = preferences.NewRequests,
                QuoteResponses = preferences.QuoteResponses,
                StatusUpdates = preferences.StatusUpdates,
                Messages = preferences.Messages,
                Reminders = preferences.Reminders
            };

            return (true, "Preferences retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for profile {ProfileId}", profileId);
            return (false, "Failed to get notification preferences", null);
        }
    }

    public async Task<(bool Success, string Message)> SendPushNotificationAsync(Guid profileId, string title, string body, Dictionary<string, object>? data = null, string priority = "normal")
    {
        try
        {
            // Get user ID from profile ID
            var userIdQuery = await _context.Requesters
                .Where(r => r.Id == profileId)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();
            
            if (userIdQuery == Guid.Empty)
            {
                userIdQuery = await _context.Providers
                    .Where(p => p.Id == profileId)
                    .Select(p => p.UserId)
                    .FirstOrDefaultAsync();
            }
            
            if (userIdQuery == Guid.Empty)
            {
                return (false, "Profile not found");
            }

            // Check user preferences
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userIdQuery);

            if (preferences?.EnablePushNotifications == false)
            {
                return (true, "User has disabled push notifications");
            }

            // Get active push tokens
            var tokens = await _context.UserPushTokens
                .Where(t => t.UserId == userIdQuery && t.IsActive)
                .ToListAsync();

            if (!tokens.Any())
            {
                return (false, "No active push tokens found for user");
            }

            var messages = tokens.Select(token => new ExpoMessage
            {
                To = token.PushToken,
                Title = title,
                Body = body,
                Data = data,
                Priority = priority,
                Sound = "default"
            }).ToList();

            var success = await SendToExpoAsync(messages);

            // Log notification
            var log = new PushNotificationLog
            {
                UserId = userIdQuery,
                Title = title,
                Body = body,
                Data = data != null ? JsonSerializer.Serialize(data) : null,
                Status = success ? "sent" : "failed"
            };

            _context.PushNotificationLogs.Add(log);
            await _context.SaveChangesAsync();

            return (success, success ? "Notification sent successfully" : "Failed to send notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to profile {ProfileId}", profileId);
            return (false, "Failed to send push notification");
        }
    }

    public async Task<(bool Success, string Message)> SendBatchNotificationsAsync(List<Guid> profileIds, string title, string body, Dictionary<string, object>? data = null)
    {
        try
        {
            // Get user IDs from profile IDs
            var userIds = await _context.Requesters
                .Where(r => profileIds.Contains(r.Id))
                .Select(r => r.UserId)
                .Union(_context.Providers
                    .Where(p => profileIds.Contains(p.Id))
                    .Select(p => p.UserId))
                .ToListAsync();

            var tokens = await _context.UserPushTokens
                .Where(t => userIds.Contains(t.UserId) && t.IsActive)
                .Join(_context.NotificationPreferences.Where(p => p.EnablePushNotifications),
                    t => t.UserId,
                    p => p.UserId,
                    (t, p) => t)
                .ToListAsync();

            if (!tokens.Any())
            {
                return (false, "No active push tokens found for users");
            }

            var messages = tokens.Select(token => new ExpoMessage
            {
                To = token.PushToken,
                Title = title,
                Body = body,
                Data = data,
                Priority = "normal",
                Sound = "default"
            }).ToList();

            var success = await SendToExpoAsync(messages);
            return (success, success ? "Batch notifications sent successfully" : "Failed to send batch notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending batch notifications");
            return (false, "Failed to send batch notifications");
        }
    }

    private async Task<bool> SendToExpoAsync(List<ExpoMessage> messages)
    {
        try
        {
            bool allSuccessful = true;
            
            foreach (var message in messages)
            {
                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ExpoApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Expo response for token {Token}: {Response}", message.To, responseContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    allSuccessful = false;
                    _logger.LogWarning("Failed to send notification to token {Token}: {Response}", message.To, responseContent);
                }
            }

            return allSuccessful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending to Expo API");
            return false;
        }
    }

    // Event-based notification methods
    public async Task NotifyNewServiceRequestAsync(Guid requestId, List<Guid> providerIds)
    {
        var request = await _context.ServiceRequests.FindAsync(requestId);
        if (request == null) return;

        await SendBatchNotificationsAsync(
            providerIds,
            "New Service Request",
            $"New {request.MainCategory} request in your area",
            new Dictionary<string, object> { { "requestId", requestId.ToString() }, { "type", "new_request" } }
        );
    }

    public async Task NotifyQuoteSubmittedAsync(Guid requestId, Guid requesterId)
    {
        var request = await _context.ServiceRequests.FindAsync(requestId);
        if (request == null) return;

        await SendPushNotificationAsync(
            requesterId,
            "New Quote Received",
            $"You received a new quote for your {request.MainCategory} request",
            new Dictionary<string, object> { { "requestId", requestId.ToString() }, { "type", "quote_received" } }
        );
    }

    public async Task NotifyQuoteResponseAsync(Guid quoteId, Guid providerId, bool isAccepted)
    {
        var status = isAccepted ? "accepted" : "rejected";
        await SendPushNotificationAsync(
            providerId,
            $"Quote {status.ToUpper()}",
            $"Your quote has been {status}",
            new Dictionary<string, object> { { "quoteId", quoteId.ToString() }, { "type", "quote_response" } }
        );
    }

    public async Task NotifyStatusChangeAsync(Guid requestId, string newStatus)
    {
        var request = await _context.ServiceRequests
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        
        if (request?.Requester?.UserId == null) return;

        await SendPushNotificationAsync(
            request.Requester.UserId,
            "Service Status Update",
            $"Your service request status changed to {newStatus}",
            new Dictionary<string, object> { { "requestId", requestId.ToString() }, { "type", "status_update" } }
        );
    }

    public async Task NotifyNewMessageAsync(Guid conversationId, Guid recipientId, string senderName)
    {
        await SendPushNotificationAsync(
            recipientId,
            "New Message",
            $"New message from {senderName}",
            new Dictionary<string, object> { { "conversationId", conversationId.ToString() }, { "type", "new_message" } }
        );
    }

    public async Task NotifyPaymentReceivedAsync(Guid paymentId, Guid providerId, decimal amount)
    {
        await SendPushNotificationAsync(
            providerId,
            "Payment Received",
            $"You received a payment of ₹{amount:F2}",
            new Dictionary<string, object> { { "paymentId", paymentId.ToString() }, { "type", "payment_received" } }
        );
    }
}