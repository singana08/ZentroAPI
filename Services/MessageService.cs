using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ZentroAPI.Hubs;

namespace ZentroAPI.Services;

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

    public async Task<(bool Success, string Message, MessagesListResponse? Data)> GetMessagesAsync(Guid requestId, Guid profileId, Guid otherUserId)
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

            // Always filter messages by conversation participants
            var messagesQuery = _context.Messages
                .Where(m => m.RequestId == requestId && 
                    ((m.SenderId == profileId && m.ReceiverId == otherUserId) ||
                     (m.SenderId == otherUserId && m.ReceiverId == profileId)));

            var messages = await messagesQuery
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

            // Get quotes for this conversation (filter by otherUserId)
            var quotesQuery = _context.Quotes
                .Include(q => q.Provider)
                .ThenInclude(p => p!.User)
                .Where(q => q.RequestId == requestId && q.ProviderId == otherUserId);

            var quotes = await quotesQuery
                .GroupJoin(_context.Agreements,
                    q => q.Id,
                    a => a.QuoteId,
                    (q, agreements) => new { Quote = q, Agreement = agreements.FirstOrDefault() })
                .Select(qa => new QuoteResponseDto
                {
                    Id = qa.Quote.Id,
                    ProviderId = qa.Quote.ProviderId,
                    RequestId = qa.Quote.RequestId,
                    Price = qa.Quote.Price,
                    Message = qa.Quote.Message,
                    CreatedAt = qa.Quote.CreatedAt,
                    ExpiresAt = qa.Quote.ExpiresAt,
                    ProviderName = qa.Quote.Provider!.User!.FullName,
                    ProviderRating = qa.Quote.Provider.Rating,
                    QuoteStatus = qa.Quote.Status,
                    IsAcceptedByRequester = qa.Agreement != null && qa.Agreement.RequesterAccepted,
                    IsAcceptedByProvider = qa.Agreement != null && qa.Agreement.ProviderAccepted
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

    public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetProviderJobsAsync(Guid providerId, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation($"Getting provider jobs for provider {providerId}, page {page}, pageSize {pageSize}");
            
            // Get requests where provider has quoted OR is assigned
            var quotedRequestIds = await _context.Quotes
                .Where(q => q.ProviderId == providerId)
                .Select(q => q.RequestId)
                .ToListAsync();

            var baseQuery = _context.ServiceRequests
                .Where(sr => quotedRequestIds.Contains(sr.Id) || sr.AssignedProviderId == providerId)
                .AsNoTracking();

            var total = await baseQuery.CountAsync();

            var serviceRequests = await baseQuery
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var requests = new List<ServiceRequestResponseDto>();
            foreach (var serviceRequest in serviceRequests)
            {
                var quotes = await _context.Quotes
                    .Include(q => q.Provider)
                    .ThenInclude(p => p!.User)
                    .Where(q => q.RequestId == serviceRequest.Id && q.ProviderId == providerId)
                    .GroupJoin(_context.Agreements,
                        q => q.Id,
                        a => a.QuoteId,
                        (q, agreements) => new { Quote = q, Agreement = agreements.FirstOrDefault() })
                    .Select(qa => new QuoteResponseDto
                    {
                        Id = qa.Quote.Id,
                        ProviderId = qa.Quote.ProviderId,
                        RequestId = qa.Quote.RequestId,
                        Price = qa.Quote.Price,
                        Message = qa.Quote.Message,
                        CreatedAt = qa.Quote.CreatedAt,
                        ExpiresAt = qa.Quote.ExpiresAt,
                        ProviderName = qa.Quote.Provider!.User!.FullName,
                        ProviderRating = qa.Quote.Provider.Rating,
                        QuoteStatus = qa.Quote.Status,
                        IsAcceptedByRequester = qa.Agreement != null && qa.Agreement.RequesterAccepted,
                        IsAcceptedByProvider = qa.Agreement != null && qa.Agreement.ProviderAccepted
                    })
                    .ToListAsync();

                // Get assigned provider name if exists
                string? assignedProviderName = null;
                if (serviceRequest.AssignedProviderId.HasValue)
                {
                    assignedProviderName = await _context.Providers
                        .Where(p => p.Id == serviceRequest.AssignedProviderId.Value)
                        .Join(_context.Users, p => p.UserId, u => u.Id, (p, u) => u.FullName)
                        .FirstOrDefaultAsync();
                }

                requests.Add(new ServiceRequestResponseDto
                {
                    Id = serviceRequest.Id,
                    UserId = serviceRequest.RequesterId,
                    BookingType = serviceRequest.BookingType.ToString(),
                    MainCategory = serviceRequest.MainCategory,
                    SubCategory = serviceRequest.SubCategory,
                    Title = serviceRequest.Title,
                    Description = serviceRequest.Description,
                    Date = serviceRequest.Date,
                    Time = serviceRequest.Time,
                    Location = serviceRequest.Location,
                    Notes = serviceRequest.Notes,
                    AdditionalNotes = serviceRequest.AdditionalNotes,
                    AssignedProviderId = serviceRequest.AssignedProviderId,
                    AssignedProviderName = assignedProviderName,
                    RequestStatus = serviceRequest.Status.ToString(),
                    QuoteCount = quotes.Count,
                    Quotes = quotes,
                    CreatedAt = serviceRequest.CreatedAt,
                    UpdatedAt = serviceRequest.UpdatedAt,
                    Coordinates = serviceRequest.Latitude.HasValue && serviceRequest.Longitude.HasValue 
                        ? new CoordinatesDto { Latitude = serviceRequest.Latitude.Value, Longitude = serviceRequest.Longitude.Value }
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

            _logger.LogInformation($"Returning {requests.Count} jobs for provider {providerId}");
            return (true, "Provider jobs retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting provider jobs for provider {providerId}: {ex.Message}");
            return (false, $"Failed to get provider jobs: {ex.Message}", null);
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

            // Get request IDs where this provider already has a quote
            var quotedRequestIds = await _context.Quotes
                .Where(q => q.ProviderId == providerId)
                .Select(q => q.RequestId)
                .ToListAsync();

            _logger.LogInformation($"Found {hiddenRequestIds.Count} hidden requests and {quotedRequestIds.Count} already quoted requests for provider {providerId}");

            // Get open/reopened requests without assigned provider OR any requests assigned to current provider
            // Exclude requests where provider already has a quote
            var baseQuery = _context.ServiceRequests
                .Where(sr => ((sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.Reopened) 
                            && sr.AssignedProviderId == null) ||
                            sr.AssignedProviderId == providerId)
                .Where(sr => !quotedRequestIds.Contains(sr.Id))
                .AsNoTracking();

            var allOpenRequests = await baseQuery.Select(sr => sr.Id).ToListAsync();
            _logger.LogInformation($"Total open/reopened requests: {allOpenRequests.Count} - [{string.Join(", ", allOpenRequests)}]");

            // Filter out hidden requests and already quoted requests
            var visibleRequestIds = allOpenRequests.Except(hiddenRequestIds).Except(quotedRequestIds).ToList();
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
                
                // Don't show any quotes for available requests - providers should only see quote count
                var quoteCount = await _context.Quotes
                    .Where(q => q.RequestId == item.ServiceRequest.Id)
                    .CountAsync();

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
                    QuoteCount = quoteCount,
                    Quotes = new List<QuoteResponseDto>(),
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

            // CRITICAL VALIDATION: Check if there's an accepted agreement
            var hasAcceptedAgreement = await _context.Agreements
                .Join(_context.Quotes, a => a.QuoteId, q => q.Id, (a, q) => new { Agreement = a, Quote = q })
                .AnyAsync(aq => aq.Quote.RequestId == request.RequestId && 
                               aq.Quote.ProviderId == providerId &&
                               aq.Agreement.RequesterAccepted && 
                               aq.Agreement.ProviderAccepted);

            if (!hasAcceptedAgreement)
            {
                _logger.LogWarning($"Attempt to complete request {request.RequestId} without accepted agreement by provider {providerId}");
                return (false, "Cannot complete request: No accepted agreement found. Both requester and provider must accept a quote before completion.");
            }

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
            // Get conversations grouped by request and other participant
            var conversations = await _context.Messages
                .Where(m => m.SenderId == profileId || m.ReceiverId == profileId)
                .GroupBy(m => new { m.RequestId, OtherUserId = m.SenderId == profileId ? m.ReceiverId : m.SenderId })
                .Select(g => new
                {
                    RequestId = g.Key.RequestId,
                    OtherUserId = g.Key.OtherUserId,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).First().MessageText,
                    LastMessageTime = g.Max(m => m.Timestamp),
                    UnreadCount = g.Count(m => m.ReceiverId == profileId && m.SenderId == g.Key.OtherUserId && !m.IsRead)
                })
                .ToListAsync();

            var chatList = new List<ChatListItemDto>();
            
            foreach (var conversation in conversations)
            {
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.Id == conversation.RequestId);
                
                if (serviceRequest?.Status == ServiceRequestStatus.Completed)
                    continue;
                
                string serviceTitle = serviceRequest != null 
                    ? $"{serviceRequest.SubCategory} - {serviceRequest.MainCategory}"
                    : "Service";
                
                // Get other user name
                string otherUserName = "User";
                var requester = await _context.Requesters
                    .Where(r => r.Id == conversation.OtherUserId)
                    .Join(_context.Users, r => r.UserId, u => u.Id, (r, u) => u.FullName)
                    .FirstOrDefaultAsync();
                
                if (requester != null)
                {
                    otherUserName = requester;
                }
                else
                {
                    var provider = await _context.Providers
                        .Where(p => p.Id == conversation.OtherUserId)
                        .Join(_context.Users, p => p.UserId, u => u.Id, (p, u) => u.FullName)
                        .FirstOrDefaultAsync();
                    
                    if (provider != null)
                        otherUserName = provider;
                }
                
                // Get quotes for this conversation
                var quotes = await _context.Quotes
                    .Include(q => q.Provider)
                    .ThenInclude(p => p!.User)
                    .Where(q => q.RequestId == conversation.RequestId && q.ProviderId == conversation.OtherUserId)
                    .GroupJoin(_context.Agreements,
                        q => q.Id,
                        a => a.QuoteId,
                        (q, agreements) => new { Quote = q, Agreement = agreements.FirstOrDefault() })
                    .Select(qa => new QuoteResponseDto
                    {
                        Id = qa.Quote.Id,
                        ProviderId = qa.Quote.ProviderId,
                        RequestId = qa.Quote.RequestId,
                        Price = qa.Quote.Price,
                        Message = qa.Quote.Message,
                        CreatedAt = qa.Quote.CreatedAt,
                        ExpiresAt = qa.Quote.ExpiresAt,
                        ProviderName = qa.Quote.Provider!.User!.FullName,
                        ProviderRating = qa.Quote.Provider.Rating,
                        QuoteStatus = qa.Quote.Status,
                        IsAcceptedByRequester = qa.Agreement != null && qa.Agreement.RequesterAccepted,
                        IsAcceptedByProvider = qa.Agreement != null && qa.Agreement.ProviderAccepted
                    })
                    .ToListAsync();
                
                chatList.Add(new ChatListItemDto
                {
                    RequestId = conversation.RequestId,
                    OtherUserId = conversation.OtherUserId,
                    OtherUserName = otherUserName,
                    ServiceTitle = serviceTitle,
                    LastMessage = conversation.LastMessage,
                    LastMessageTime = conversation.LastMessageTime,
                    UnreadCount = conversation.UnreadCount,
                    IsOnline = false,
                    Quotes = quotes,
                    RequestStatus = serviceRequest?.Status.ToString() ?? "Unknown"
                });
            }
            
            chatList = chatList.OrderByDescending(c => c.LastMessageTime).ToList();

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
