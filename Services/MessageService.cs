using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ApplicationDbContext context, ILogger<MessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, MessageResponseDto? Data)> SendMessageAsync(Guid senderId, SendMessageDto request)
    {
        try
        {
            // Verify service request exists
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            // Auto-detect receiver if not provided
            Guid receiverId;
            if (request.ReceiverId.HasValue)
            {
                receiverId = request.ReceiverId.Value;
            }
            else
            {
                // If sender is requester, receiver is assigned provider (or any provider for open requests)
                // If sender is provider, receiver is requester
                if (serviceRequest.RequesterId == senderId)
                {
                    receiverId = serviceRequest.AssignedProviderId ?? Guid.Empty;
                    if (receiverId == Guid.Empty)
                        return (false, "No provider assigned to send message to", null);
                }
                else
                {
                    receiverId = serviceRequest.RequesterId;
                }
            }

            // Verify sender has access to this request
            var isRequester = serviceRequest.RequesterId == senderId;
            var isAssignedProvider = serviceRequest.AssignedProviderId == senderId;
            var isProvider = await _context.Providers.AnyAsync(p => p.Id == senderId);
            var isOpenForNegotiation = serviceRequest.Status == ServiceRequestStatus.Open || serviceRequest.Status == ServiceRequestStatus.Reopened;
            
            bool hasAccess = isRequester || isAssignedProvider || (isProvider && isOpenForNegotiation);
            if (!hasAccess)
                return (false, "Access denied", null);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                RequestId = request.RequestId,
                MessageText = request.MessageText
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var response = new MessageResponseDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                RequestId = message.RequestId,
                MessageText = message.MessageText,
                Timestamp = message.Timestamp,
                IsRead = message.IsRead
            };

            return (true, "Message sent successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return (false, "Failed to send message", null);
        }
    }

    public async Task<(bool Success, string Message, MessagesListResponse? Data)> GetMessagesAsync(Guid requestId, Guid profileId)
    {
        try
        {
            // Verify access to request
            var serviceRequest = await _context.ServiceRequests.FindAsync(requestId);
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            // Check if user is requester, assigned provider, or any provider for open requests
            var isRequester = serviceRequest.RequesterId == profileId;
            var isAssignedProvider = serviceRequest.AssignedProviderId == profileId;
            var isProvider = await _context.Providers.AnyAsync(p => p.Id == profileId);
            var isOpenRequest = serviceRequest.Status == ServiceRequestStatus.Open || serviceRequest.Status == ServiceRequestStatus.Reopened;
            
            bool hasAccess = isRequester || isAssignedProvider || (isProvider && isOpenRequest);
            if (!hasAccess)
                return (false, "Access denied", null);

            var messages = await _context.Messages
                .Where(m => m.RequestId == requestId && 
                       (isRequester || // Requesters see all messages
                        m.SenderId == profileId || m.ReceiverId == profileId)) // Providers see only their own conversation
                .OrderBy(m => m.Timestamp)
                .Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    RequestId = m.RequestId,
                    MessageText = m.MessageText,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead
                })
                .ToListAsync();

            var unreadCount = messages.Count(m => m.ReceiverId == profileId && !m.IsRead);

            var response = new MessagesListResponse
            {
                Messages = messages,
                TotalCount = messages.Count,
                UnreadCount = unreadCount
            };

            return (true, "Messages retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages");
            return (false, "Failed to get messages", null);
        }
    }

    public async Task<(bool Success, string Message)> AssignProviderAsync(Guid requesterId, AssignProviderDto request)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found");

            if (serviceRequest.RequesterId != requesterId)
                return (false, "Access denied");

            if (serviceRequest.Status != ServiceRequestStatus.Open && serviceRequest.Status != ServiceRequestStatus.Reopened)
                return (false, "Service request is not available for assignment");

            var provider = await _context.Providers.FindAsync(request.ProviderId);
            if (provider == null)
                return (false, "Provider not found");

            serviceRequest.AssignedProviderId = request.ProviderId;
            serviceRequest.Status = ServiceRequestStatus.Assigned;
            serviceRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Provider assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning provider");
            return (false, "Failed to assign provider");
        }
    }

    public async Task<(bool Success, string Message)> RejectRequestAsync(Guid providerId, RequestActionDto request)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found");

            if (serviceRequest.AssignedProviderId != providerId)
                return (false, "Access denied");

            serviceRequest.Status = ServiceRequestStatus.Rejected;
            serviceRequest.AssignedProviderId = null;
            serviceRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Request rejected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request");
            return (false, "Failed to reject request");
        }
    }

    public async Task<(bool Success, string Message)> ReopenRequestAsync(Guid requesterId, RequestActionDto request)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found");

            if (serviceRequest.RequesterId != requesterId)
                return (false, "Access denied");

            if (serviceRequest.Status != ServiceRequestStatus.Rejected && serviceRequest.Status != ServiceRequestStatus.Cancelled)
                return (false, "Request cannot be reopened");

            serviceRequest.Status = ServiceRequestStatus.Reopened;
            serviceRequest.AssignedProviderId = null;
            serviceRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Request reopened successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening request");
            return (false, "Failed to reopen request");
        }
    }

    public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetOpenRequestsAsync(Guid providerId, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation($"Getting open requests for provider {providerId}, page {page}, pageSize {pageSize}");
            
            // Get hidden request IDs for this provider using ProviderRequestStatus
            var hiddenRequestIds = await _context.ProviderRequestStatuses
                .Where(prs => prs.ProviderId == providerId && prs.Status == ProviderStatus.Hidden)
                .Select(prs => prs.RequestId)
                .ToListAsync();

            _logger.LogInformation($"Found {hiddenRequestIds.Count} hidden requests for provider {providerId}");

            var query = _context.ServiceRequests
                .Where(sr => (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.Reopened) 
                            && sr.AssignedProviderId == null
                            && !hiddenRequestIds.Contains(sr.Id))
                .AsNoTracking();

            var total = await query.CountAsync();
            _logger.LogInformation($"Found {total} total open requests for provider {providerId}");

            var requests = await query
                .GroupJoin(_context.ProviderRequestStatuses.Where(prs => prs.ProviderId == providerId),
                    sr => sr.Id,
                    prs => prs.RequestId,
                    (sr, prs) => new { ServiceRequest = sr, ProviderStatus = prs.FirstOrDefault() })
                .OrderByDescending(x => x.ServiceRequest.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ServiceRequestResponseDto
                {
                    Id = x.ServiceRequest.Id,
                    UserId = x.ServiceRequest.RequesterId,
                    BookingType = x.ServiceRequest.BookingType.ToString(),
                    MainCategory = x.ServiceRequest.MainCategory,
                    SubCategory = x.ServiceRequest.SubCategory,
                    Title = x.ServiceRequest.Title,
                    Description = x.ServiceRequest.Description,
                    Date = x.ServiceRequest.Date,
                    Time = x.ServiceRequest.Time,
                    Location = x.ServiceRequest.Location,
                    Notes = x.ServiceRequest.Notes,
                    RequestStatus = x.ServiceRequest.Status.ToString(),
                    ProviderStatus = x.ProviderStatus != null ? x.ProviderStatus.Status.ToString() : "Viewed",
                    CreatedAt = x.ServiceRequest.CreatedAt,
                    UpdatedAt = x.ServiceRequest.UpdatedAt
                })
                .ToListAsync();

            var response = new PaginatedServiceRequestsDto
            {
                Data = requests,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation($"Returning {requests.Count} requests for provider {providerId}");
            return (true, "Open requests retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting open requests for provider {providerId}: {ex.Message}");
            return (false, $"Failed to get open requests: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ProviderStatusResponseDto? Data)> UpdateProviderStatusAsync(Guid providerId, UpdateProviderStatusDto request)
    {
        try
        {
            if (!Enum.TryParse<ProviderStatus>(request.Status, true, out var status))
                return (false, "Invalid status", null);

            var existingStatus = await _context.ProviderRequestStatuses
                .FirstOrDefaultAsync(prs => prs.ProviderId == providerId && prs.RequestId == request.RequestId);

            if (existingStatus == null)
            {
                existingStatus = new ProviderRequestStatus
                {
                    ProviderId = providerId,
                    RequestId = request.RequestId,
                    Status = status,
                    QuoteId = request.QuoteId
                };
                _context.ProviderRequestStatuses.Add(existingStatus);
            }
            else
            {
                existingStatus.Status = status;
                existingStatus.LastUpdated = DateTime.UtcNow;
                existingStatus.QuoteId = request.QuoteId;
            }

            await _context.SaveChangesAsync();

            var response = new ProviderStatusResponseDto
            {
                Id = existingStatus.Id,
                RequestId = existingStatus.RequestId,
                ProviderId = existingStatus.ProviderId,
                Status = existingStatus.Status.ToString(),
                LastUpdated = existingStatus.LastUpdated,
                QuoteId = existingStatus.QuoteId
            };

            return (true, "Status updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider status");
            return (false, "Failed to update status", null);
        }
    }

    public async Task<(bool Success, string Message, ProviderStatusResponseDto? Data)> GetProviderStatusAsync(Guid providerId, Guid requestId)
    {
        try
        {
            var status = await _context.ProviderRequestStatuses
                .FirstOrDefaultAsync(prs => prs.ProviderId == providerId && prs.RequestId == requestId);

            if (status == null)
                return (false, "Status not found", null);

            var response = new ProviderStatusResponseDto
            {
                Id = status.Id,
                RequestId = status.RequestId,
                ProviderId = status.ProviderId,
                Status = status.Status.ToString(),
                LastUpdated = status.LastUpdated,
                QuoteId = status.QuoteId
            };

            return (true, "Status retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider status");
            return (false, "Failed to get status", null);
        }
    }

    public async Task<(bool Success, string Message)> CompleteRequestAsync(Guid providerId, RequestActionDto request)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found");

            if (serviceRequest.AssignedProviderId != providerId)
                return (false, "Access denied");

            serviceRequest.Status = ServiceRequestStatus.Completed;
            serviceRequest.UpdatedAt = DateTime.UtcNow;

            var providerStatus = await _context.ProviderRequestStatuses
                .FirstOrDefaultAsync(prs => prs.ProviderId == providerId && prs.RequestId == request.RequestId);
            
            if (providerStatus != null)
            {
                providerStatus.Status = ProviderStatus.Completed;
                providerStatus.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return (true, "Request completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing request");
            return (false, "Failed to complete request");
        }
    }

    public async Task<(bool Success, string Message, ChatListResponse? Data)> GetChatListAsync(Guid profileId)
    {
        try
        {
            var chatList = await _context.Messages
                .Where(m => m.SenderId == profileId || m.ReceiverId == profileId)
                .GroupBy(m => new { m.RequestId, OtherUserId = m.SenderId == profileId ? m.ReceiverId : m.SenderId })
                .Select(g => new
                {
                    RequestId = g.Key.RequestId,
                    OtherUserId = g.Key.OtherUserId,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).First().MessageText,
                    LastMessageTime = g.Max(m => m.Timestamp),
                    UnreadCount = g.Count(m => m.ReceiverId == profileId && !m.IsRead)
                })
                .Join(_context.ServiceRequests,
                    chat => chat.RequestId,
                    sr => sr.Id,
                    (chat, sr) => new { chat, ServiceTitle = $"{sr.SubCategory} - {sr.MainCategory}" })
                .Join(_context.Users,
                    combined => combined.chat.OtherUserId,
                    user => user.Id,
                    (combined, user) => new ChatListItemDto
                    {
                        RequestId = combined.chat.RequestId,
                        OtherUserId = combined.chat.OtherUserId,
                        OtherUserName = user.FullName,
                        ServiceTitle = combined.ServiceTitle,
                        LastMessage = combined.chat.LastMessage,
                        LastMessageTime = combined.chat.LastMessageTime,
                        UnreadCount = combined.chat.UnreadCount,
                        IsOnline = false
                    })
                .OrderByDescending(c => c.LastMessageTime)
                .ToListAsync();

            var response = new ChatListResponse
            {
                Chats = chatList,
                TotalCount = chatList.Count
            };

            return (true, "Chat list retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat list");
            return (false, "Failed to get chat list", null);
        }
    }
}