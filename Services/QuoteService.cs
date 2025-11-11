using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

public class QuoteService : IQuoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(ApplicationDbContext context, ILogger<QuoteService> logger)
    {
        _context = context;
        _logger = logger;
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
                return (false, "Quote already submitted for this request", null);

            var quote = new Quote
            {
                ProviderId = providerId,
                RequestId = request.RequestId,
                Price = request.Price,
                Message = request.Message
            };

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            var response = new QuoteResponseDto
            {
                Id = quote.Id,
                ProviderId = quote.ProviderId,
                RequestId = quote.RequestId,
                Price = quote.Price,
                Message = quote.Message,
                CreatedAt = quote.CreatedAt
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

            if (serviceRequest.RequesterId != profileId)
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