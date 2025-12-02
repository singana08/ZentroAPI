using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Interface for review service
/// </summary>
public interface IReviewService
{
    Task<(bool Success, string Message, ReviewResponseDto? Data)> CreateReviewAsync(
        Guid customerId, 
        CreateReviewDto request);
        
    Task<(bool Success, string Message, ProviderRatingsResponseDto? Data)> GetProviderRatingsAsync(
        Guid providerId);
}
