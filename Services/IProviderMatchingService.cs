using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface IProviderMatchingService
{
    Task<List<ProviderMatchDto>> FindMatchingProvidersAsync(Guid serviceRequestId);
    Task<List<ProviderMatchDto>> GetRecommendedProvidersAsync(
        string category, double? latitude = null, double? longitude = null, int maxResults = 10);
    Task<decimal> CalculateMatchScoreAsync(Guid providerId, Guid serviceRequestId);
}