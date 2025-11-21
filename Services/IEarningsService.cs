using HaluluAPI.DTOs;

namespace HaluluAPI.Services;

/// <summary>
/// Interface for provider earnings service
/// </summary>
public interface IEarningsService
{
    Task<(bool Success, string Message, ProviderEarningsResponseDto? Data)> GetProviderEarningsAsync(
        Guid providerId, 
        string? period = null);
}