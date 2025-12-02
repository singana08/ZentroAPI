using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Interface for dashboard service
/// </summary>
public interface IDashboardService
{
    Task<(bool Success, string Message, ProviderDashboardResponseDto? Data)> GetProviderDashboardAsync(
        Guid providerId);
}
