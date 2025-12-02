using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Interface for provider analytics service
/// </summary>
public interface IAnalyticsService
{
    Task<(bool Success, string Message, ProviderAnalyticsResponseDto? Data)> GetProviderAnalyticsAsync(
        Guid providerId);
}
