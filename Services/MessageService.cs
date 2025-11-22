using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HaluluAPI.Hubs;

namespace HaluluAPI.Services;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessageService> _logger;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageService(ApplicationDbContext context, ILogger<MessageService> logger, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
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
                // If sender is requester, find the other person from existing messages
                // If sender is provider, receiver is requester
                if (serviceRequest.RequesterId == senderId)
                {
                    // For requesters: find the other person from existing messages in this conversation
                    var lastMessage = await _context.Messages
                        .Where(m => m.RequestId == request.RequestId && m.SenderId != senderId)
                        .OrderByDescending(m => m.Timestamp)
                        .FirstOrDefaultAsync();
                    
                    if (lastMessage != null)
                    {
                        receiverId = lastMessage.SenderId;
                    }
                    else
                    {
                        // Fallback to assigned provider if no messages exist
                        receiverId = serviceRequest.AssignedProviderId ?? Guid.Empty;
                        if (receiverId == Guid.Empty)
                            return (false, "No provider to send message to", null);
                    }
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

            // Send real-time notification via SignalR
            await _hubContext.Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", response);
            await _hubContext.Clients.Group($"request_{request.RequestId}").SendAsync("ReceiveMessage", response);

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
            // Verify access to request - use AsNoTracking to get fresh data
            var serviceRequest = await _context.ServiceRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.Id == requestId);
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
                .Where(m => m.RequestId == requestId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    RequestId = m.RequestId,
                    MessageText = m.MessageText,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead,
                    IsOwn = m.SenderId == profileId
                })
                .ToListAsync();

            var unreadCount = messages.Count(m => m.ReceiverId == profileId && !m.IsRead);

            // Get quotes for this request
            var quotes = await _context.Quotes
                .Include(q => q.Provider)
                .ThenInclude(p => p!.User)
                .Where(q => q.RequestId == requestId)
                .Select(q => new QuoteResponseDto
                {
                    Id = q.Id,
                    ProviderId = q.ProviderId,
                    RequestId = q.RequestId,
                    Price = q.Price,
                    Message = q.Message,
                    CreatedAt = q.CreatedAt,
                    ExpiresAt = q.ExpiresAt,
                    ProviderName = q.Provider!.User!.FullName,
                    ProviderRating = q.Provider.Rating
                })
                .ToListAsync();

            _logger.LogInformation($"Service request {requestId} status: {serviceRequest.Status}, AssignedProviderId: {serviceRequest.AssignedProviderId}");

            var response = new MessagesListResponse
            {
                Messages = messages,
                TotalCount = messages.Count,
                UnreadCount = unreadCount,
                ProviderId = serviceRequest.AssignedProviderId,
                Quotes = quotes,
                RequestStatus = serviceRequest.Status.ToString()
            };

            _logger.LogInformation($"Returning RequestStatus: {response.RequestStatus} for request {requestId}");

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

            // Create initial workflow status record
            var workflowStatus = new WorkflowStatus
            {
                RequestId = request.RequestId,
                ProviderId = request.ProviderId,
                IsAssigned = true,
                AssignedDate = DateTime.UtcNow
            };
            _context.WorkflowStatuses.Add(workflowStatus);

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

            _logger.LogInformation($"Found {hiddenRequestIds.Count} hidden requests for provider {providerId}: [{string.Join(", ", hiddenRequestIds)}]");

            // Get open/reopened requests without assigned provider OR any requests assigned to current provider
            var baseQuery = _context.ServiceRequests
                .Where(sr => ((sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.Reopened) 
                            && sr.AssignedProviderId == null) ||
                            sr.AssignedProviderId == providerId)
                .AsNoTracking();

            var allOpenRequests = await baseQuery.Select(sr => sr.Id).ToListAsync();
            _logger.LogInformation($"Total open/reopened requests: {allOpenRequests.Count} - [{string.Join(", ", allOpenRequests)}]");

            // Filter out hidden requests
            var visibleRequestIds = allOpenRequests.Except(hiddenRequestIds).ToList();
            _logger.LogInformation($"Visible requests after filtering: {visibleRequestIds.Count} - [{string.Join(", ", visibleRequestIds)}]");

            var total = visibleRequestIds.Count;

            // Get paginated visible requests with their provider status
            var serviceRequests = await _context.ServiceRequests
                .Where(sr => visibleRequestIds.Contains(sr.Id))
                .GroupJoin(_context.ProviderRequestStatuses.Where(prs => prs.ProviderId == providerId),
                    sr => sr.Id,
                    prs => prs.RequestId,
                    (sr, prs) => new { ServiceRequest = sr, ProviderStatus = prs.FirstOrDefault() })
                .OrderByDescending(x => x.ServiceRequest.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var requests = new List<ServiceRequestResponseDto>();
            foreach (var item in serviceRequests)
            {
                var currentProviderStatus = item.ProviderStatus;
                
                // Don't auto-create status record - show as "New"
                // Don't auto-update if status already exists - maintain hierarchy
                
                // Get quotes for this request
                var quotes = await _context.Quotes
                    .Include(q => q.Provider)
                    .ThenInclude(p => p!.User)
                    .Where(q => q.RequestId == item.ServiceRequest.Id)
                    .Select(q => new QuoteResponseDto
                    {
                        Id = q.Id,
                        ProviderId = q.ProviderId,
                        RequestId = q.RequestId,
                        Price = q.Price,
                        Message = q.Message,
                        CreatedAt = q.CreatedAt,
                        ExpiresAt = q.ExpiresAt,
                        ProviderName = q.Provider!.User!.FullName,
                        ProviderRating = q.Provider.Rating
                    })
                    .ToListAsync();

                requests.Add(new ServiceRequestResponseDto
                {
                    Id = item.ServiceRequest.Id,
                    UserId = item.ServiceRequest.RequesterId,
                    BookingType = item.ServiceRequest.BookingType.ToString(),
                    MainCategory = item.ServiceRequest.MainCategory,
                    SubCategory = item.ServiceRequest.SubCategory,
                    Title = item.ServiceRequest.Title,
                    Description = item.ServiceRequest.Description,
                    Date = item.ServiceRequest.Date,
                    Time = item.ServiceRequest.Time,
                    Location = item.ServiceRequest.Location,
                    Notes = item.ServiceRequest.Notes,
                    AdditionalNotes = item.ServiceRequest.AdditionalNotes,
                    AssignedProviderId = item.ServiceRequest.AssignedProviderId,
                    RequestStatus = item.ServiceRequest.Status.ToString(),
                    ProviderStatus = currentProviderStatus != null ? currentProviderStatus.Status.ToString() : "New",
                    QuoteCount = quotes.Count,
                    Quotes = quotes,
                    CreatedAt = item.ServiceRequest.CreatedAt,
                    UpdatedAt = item.ServiceRequest.UpdatedAt,
                    Coordinates = item.ServiceRequest.Latitude.HasValue && item.ServiceRequest.Longitude.HasValue 
                        ? new CoordinatesDto { Latitude = item.ServiceRequest.Latitude.Value, Longitude = item.ServiceRequest.Longitude.Value }
                        : null
                });
            }

            var response = new PaginatedServiceRequestsDto
            {
                Data = requests,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation($"Returning {requests.Count} requests out of {total} total visible requests for provider {providerId}");
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
                // Check status hierarchy - prevent downgrading unless cancelled/reopened
                if (!CanUpdateStatus(existingStatus.Status, status))
                {
                    _logger.LogInformation($"Status update blocked: Cannot change from {existingStatus.Status} to {status} for provider {providerId}, request {request.RequestId}");
                    
                    // Return current status without updating
                    var currentResponse = new ProviderStatusResponseDto
                    {
                        Id = existingStatus.Id,
                        RequestId = existingStatus.RequestId,
                        ProviderId = existingStatus.ProviderId,
                        Status = existingStatus.Status.ToString(),
                        LastUpdated = existingStatus.LastUpdated,
                        QuoteId = existingStatus.QuoteId
                    };
                    return (true, "Status maintained at higher level", currentResponse);
                }
                
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

    /// <summary>
    /// Check if status can be updated based on hierarchy rules
    /// </summary>
    private bool CanUpdateStatus(ProviderStatus currentStatus, ProviderStatus newStatus)
    {
        // Status hierarchy (higher number = higher priority):
        // Hidden = 0, Viewed = 1, Negotiating = 2, Quoted = 3, Assigned = 4, Rejected = 5, Completed = 6
        
        // Allow updates to same or higher status
        if ((int)newStatus >= (int)currentStatus)
            return true;
            
        // Allow downgrade only for specific cases:
        // - From any status to Hidden (provider can always hide)
        // - From Rejected back to lower statuses (reopening scenario)
        if (newStatus == ProviderStatus.Hidden || currentStatus == ProviderStatus.Rejected)
            return true;
            
        return false;
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
            // Get basic chat data first
            var basicChats = await _context.Messages
                .Where(m => m.SenderId == profileId || m.ReceiverId == profileId)
                .GroupBy(m => m.RequestId)
                .Select(g => new
                {
                    RequestId = g.Key,
                    OtherProfileId = g.First().SenderId == profileId ? g.First().ReceiverId : g.First().SenderId,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).First().MessageText,
                    LastMessageTime = g.Max(m => m.Timestamp),
                    UnreadCount = g.Count(m => m.ReceiverId == profileId && !m.IsRead)
                })
                .ToListAsync();

            // Get service categories and customer names
            var chatList = new List<ChatListItemDto>();
            foreach (var chat in basicChats)
            {
                var serviceRequest = await _context.ServiceRequests
                    .Where(sr => sr.Id == chat.RequestId)
                    .FirstOrDefaultAsync();
                
                string serviceTitle = "Service";
                string customerName = "User";
                
                if (serviceRequest != null)
                {
                    // Skip completed service requests
                    if (serviceRequest.Status == ServiceRequestStatus.Completed)
                        continue;
                        
                    serviceTitle = $"{serviceRequest.SubCategory} - {serviceRequest.MainCategory}";
                }
                
                // Get quotes for this request
                var quotes = await _context.Quotes
                    .Include(q => q.Provider)
                    .ThenInclude(p => p!.User)
                    .Where(q => q.RequestId == chat.RequestId)
                    .Select(q => new QuoteResponseDto
                    {
                        Id = q.Id,
                        ProviderId = q.ProviderId,
                        RequestId = q.RequestId,
                        Price = q.Price,
                        Message = q.Message,
                        CreatedAt = q.CreatedAt,
                        ExpiresAt = q.ExpiresAt,
                        ProviderName = q.Provider!.User!.FullName,
                        ProviderRating = q.Provider.Rating
                    })
                    .ToListAsync();
                
                // Get customer name from other profile
                var requester = await _context.Requesters
                    .Where(r => r.Id == chat.OtherProfileId)
                    .Join(_context.Users, r => r.UserId, u => u.Id, (r, u) => u.FullName)
                    .FirstOrDefaultAsync();
                
                if (requester != null)
                {
                    customerName = requester;
                }
                else
                {
                    var provider = await _context.Providers
                        .Where(p => p.Id == chat.OtherProfileId)
                        .Join(_context.Users, p => p.UserId, u => u.Id, (p, u) => u.FullName)
                        .FirstOrDefaultAsync();
                    
                    if (provider != null)
                        customerName = provider;
                }
                
                chatList.Add(new ChatListItemDto
                {
                    RequestId = chat.RequestId,
                    OtherUserId = chat.OtherProfileId,
                    OtherUserName = customerName,
                    ServiceTitle = serviceTitle,
                    LastMessage = chat.LastMessage,
                    LastMessageTime = chat.LastMessageTime,
                    UnreadCount = chat.UnreadCount,
                    IsOnline = false,
                    Quotes = quotes,
                    RequestStatus = serviceRequest?.Status.ToString() ?? "Unknown"
                });
            }
            
            chatList = chatList.OrderByDescending(c => c.LastMessageTime).ToList();

            // TODO: Uncomment joins later if needed for user names and service titles
            /*
            .Join(_context.ServiceRequests,
                chat => chat.RequestId,
                sr => sr.Id,
                (chat, sr) => new { chat, ServiceTitle = $"{sr.SubCategory} - {sr.MainCategory}" })
            .Join(_context.Users,
                combined => combined.chat.OtherUserId,
                user => user.Id,
                (combined, user) => new ChatListItemDto { ... })
            */

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

    public async Task<(bool Success, string Message)> MarkMessagesAsReadAsync(Guid requestId, Guid profileId)
    {
        try
        {
            var messages = await _context.Messages
                .Where(m => m.RequestId == requestId && m.ReceiverId == profileId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return (true, "Messages marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read");
            return (false, "Failed to mark messages as read");
        }
    }
}