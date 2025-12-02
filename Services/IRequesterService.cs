using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Service interface for requester operations
/// </summary>
public interface IRequesterService
{
    /// <summary>
    /// Register a new requester profile for a user
    /// </summary>
    Task<RequesterDto> RegisterRequester(Guid userId, CreateRequesterDto dto);

    /// <summary>
    /// Get requester profile by user ID
    /// </summary>
    Task<RequesterDto?> GetRequesterByUserId(Guid userId);

    /// <summary>
    /// Get requester profile by requester ID
    /// </summary>
    Task<RequesterDto?> GetRequesterById(Guid requesterId);

    /// <summary>
    /// Update requester profile
    /// </summary>
    Task<RequesterDto> UpdateRequesterProfile(Guid userId, UpdateRequesterDto dto);

    /// <summary>
    /// Check if user has an active requester profile
    /// </summary>
    Task<bool> IsActiveRequester(Guid userId);

    /// <summary>
    /// Deactivate requester profile
    /// </summary>
    Task<bool> DeactivateRequester(Guid userId);

    /// <summary>
    /// Activate requester profile
    /// </summary>
    Task<bool> ActivateRequester(Guid userId);

    /// <summary>
    /// Get requester profile with statistics
    /// </summary>
    Task<RequesterProfileDto?> GetRequesterProfileWithStats(Guid userId);
}
