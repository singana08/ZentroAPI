using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface IQuoteService
{
    Task<(bool Success, string Message, QuoteResponseDto? Data)> CreateQuoteAsync(Guid providerId, CreateQuoteDto request);
    Task<(bool Success, string Message, QuotesListResponse? Data)> GetQuotesForRequestAsync(Guid requestId, Guid profileId);
}
