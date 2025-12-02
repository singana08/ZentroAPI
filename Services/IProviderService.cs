using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Service interface for provider operations
/// </summary>
public interface IProviderService
{
    /// <summary>
    /// Register a new provider profile for a user
    /// </summary>
    Task<ProviderDto> RegisterProvider(Guid userId, CreateProviderDto dto);

    /// <summary>
    /// Get provider profile by user ID
    /// </summary>
    Task<ProviderDto?> GetProviderByUserId(Guid userId);

    /// <summary>
    /// Get provider profile by provider ID
    /// </summary>
    Task<ProviderDto?> GetProviderById(Guid providerId);

    /// <summary>
    /// Update provider profile
    /// </summary>
    Task<ProviderDto> UpdateProviderProfile(Guid userId, UpdateProviderDto dto);

    /// <summary>
    /// Check if user has an active provider profile
    /// </summary>
    Task<bool> IsActiveProvider(Guid userId);

    /// <summary>
    /// Deactivate provider profile
    /// </summary>
    Task<bool> DeactivateProvider(Guid userId);

    /// <summary>
    /// Activate provider profile
    /// </summary>
    Task<bool> ActivateProvider(Guid userId);

    /// <summary>
    /// Get provider profile with statistics
    /// </summary>
    Task<ProviderProfileDto?> GetProviderProfileWithStats(Guid userId);

    /// <summary>
    /// Search providers by category and service area
    /// </summary>
    Task<List<ProviderListDto>> SearchProviders(string? category, string? serviceArea, decimal? minRating = null);

    /// <summary>
    /// Update provider rating based on reviews
    /// </summary>
    Task<bool> UpdateProviderRating(Guid providerId, decimal newRating);

    /// <summary>
    /// Update provider earnings
    /// </summary>
    Task<bool> UpdateProviderEarnings(Guid providerId, decimal amount);

    /// <summary>
    /// Get provider availability
    /// </summary>
    Task<Dictionary<string, AvailabilitySlotDto>?> GetProviderAvailability(Guid providerId);

    /// <summary>
    /// Update provider availability
    /// </summary>
    Task<bool> UpdateProviderAvailability(Guid userId, List<AvailabilitySlotDto> slots);

    /// <summary>
    /// Get all active providers
    /// </summary>
    Task<List<ProviderListDto>> GetAllActiveProviders(int page = 1, int pageSize = 20);
}
