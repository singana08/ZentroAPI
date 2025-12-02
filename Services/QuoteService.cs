using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ZentroAPI.Hubs;

namespace ZentroAPI.Services;

public class QuoteService : IQuoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuoteService> _logger;
    private readonly IHubContext<ChatHub> _hubContext;

    public QuoteService(ApplicationDbContext context, ILogger<QuoteService> logger, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<(bool Success, string Message, QuoteResponseDto? Data)> CreateQuoteAsync(Guid providerId, CreateQuoteDto request)
    {
        try
        {
            // Check if provider exists
            var provider = await _context.Providers.FindAsync(providerId);
            if (provider == null)
                return (false, "Provider not found", null);

            // Check if service request exists and is open
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            if (serviceRequest.Status != ServiceRequestStatus.Open && serviceRequest.Status != ServiceRequestStatus.Reopened)
                return (false, "Service request is not available for quotes", null);

            // Check if quote already exists
            var existingQuote = await _context.Quotes
                .FirstOrDefaultAsync(q => q.ProviderId == providerId && q.RequestId == request.RequestId);
            if (existingQuote != null)
                return (true, "Quote already submitted for this request", null);

            var quote = new Quote
            {
                ProviderId = providerId,
                RequestId = request.RequestId,
                Price = request.Price,
                Message = request.Message
            };

            _context.Quotes.Add(quote);
            
            // Update provider status to "Quoted"
            var providerStatus = await _context.ProviderRequestStatuses
                .FirstOrDefaultAsync(prs => prs.ProviderId == providerId && prs.RequestId == request.RequestId);
            
            if (providerStatus == null)
            {
                providerStatus = new ProviderRequestStatus
                {
                    ProviderId = providerId,
                    RequestId = request.RequestId,
                    Status = ProviderStatus.Quoted,
                    QuoteId = quote.Id
                };
                _context.ProviderRequestStatuses.Add(providerStatus);
            }
            else
            {
                providerStatus.Status = ProviderStatus.Quoted;
                providerStatus.QuoteId = quote.Id;
                providerStatus.LastUpdated = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();

            // Send default message to requester
            var defaultMessage = new Message
            {
                SenderId = providerId,
                ReceiverId = serviceRequest.RequesterId,
                RequestId = request.RequestId,
                MessageText = $"Hi! I've submitted a quote of ₹{request.Price:F2} for your {serviceRequest.SubCategory} request. {request.Message ?? "I'm ready to help you with this project. Please let me know if you have any questions!"}"
            };

            _context.Messages.Add(defaultMessage);
            await _context.SaveChangesAsync();

            // Send real-time notification
            var messageResponse = new MessageResponseDto
            {
                Id = defaultMessage.Id,
                SenderId = defaultMessage.SenderId,
                ReceiverId = defaultMessage.ReceiverId,
                RequestId = defaultMessage.RequestId,
                MessageText = defaultMessage.MessageText,
                Timestamp = defaultMessage.Timestamp,
                IsRead = defaultMessage.IsRead
            };

            await _hubContext.Clients.Group($"user_{serviceRequest.RequesterId}").SendAsync("ReceiveMessage", messageResponse);
            await _hubContext.Clients.Group($"request_{request.RequestId}").SendAsync("ReceiveMessage", messageResponse);

            var response = new QuoteResponseDto
            {
                Id = quote.Id,
                ProviderId = quote.ProviderId,
                RequestId = quote.RequestId,
                Price = quote.Price,
                Message = quote.Message,
                CreatedAt = quote.CreatedAt,
                ExpiresAt = quote.ExpiresAt,
                QuoteStatus = quote.Status,
                UpdatedAt = quote.UpdatedAt
            };

            return (true, "Quote submitted successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote");
            return (false, "Failed to create quote", null);
        }
    }

    public async Task<(bool Success, string Message, QuotesListResponse? Data)> GetQuotesForRequestAsync(Guid requestId, Guid profileId)
    {
        try
        {
            // Verify request exists and user has access
            var serviceRequest = await _context.ServiceRequests.FindAsync(requestId);
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            // Check if user is requester or provider
            var isRequester = serviceRequest.RequesterId == profileId;
            var isProvider = await _context.Providers.AnyAsync(p => p.Id == profileId);
            
            if (!isRequester && !isProvider)
                return (false, "Access denied", null);

            var quotes = await _context.Quotes
                .Include(q => q.Provider)
                .ThenInclude(p => p!.User)
                .Where(q => q.RequestId == requestId)
                .OrderBy(q => q.CreatedAt)
                .Select(q => new QuoteResponseDto
                {
                    Id = q.Id,
                    ProviderId = q.ProviderId,
                    RequestId = q.RequestId,
                    Price = q.Price,
                    Message = q.Message,
                    CreatedAt = q.CreatedAt,
                    ExpiresAt = q.ExpiresAt,
                    QuoteStatus = q.Status,
                    UpdatedAt = q.UpdatedAt,
                    ProviderName = q.Provider!.User!.FullName,
                    ProviderRating = q.Provider.Rating
                })
                .ToListAsync();

            var response = new QuotesListResponse
            {
                Quotes = quotes,
                TotalCount = quotes.Count
            };

            return (true, "Quotes retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quotes");
            return (false, "Failed to get quotes", null);
        }
    }
}
