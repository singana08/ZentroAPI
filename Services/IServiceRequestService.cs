using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

/// <summary>
/// Service interface for managing service requests
/// </summary>
public interface IServiceRequestService
{
    /// <summary>
    /// Create a new service request
    /// </summary>
    /// <param name="userId">User ID creating the request</param>
    /// <param name="request">Service request details</param>
    /// <returns>Created service request response</returns>
    Task<(bool Success, string Message, ServiceRequestResponseDto? Data)> CreateServiceRequestAsync(
        Guid userId,
        CreateServiceRequestDto request);

    /// <summary>
    /// Update an existing service request
    /// </summary>
    /// <param name="requestId">Service request ID to update</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="request">Updated request details</param>
    /// <returns>Updated service request response</returns>
    Task<(bool Success, string Message, ServiceRequestResponseDto? Data)> UpdateServiceRequestAsync(
        Guid requestId,
        Guid userId,
        CreateServiceRequestDto request);



    /// <summary>
    /// Get all service requests for a specific user with pagination and filtering
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="bookingType">Optional booking type filter</param>
    /// <param name="category">Optional category filter</param>
    /// <returns>Paginated list of service requests</returns>
    Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetUserServiceRequestsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 10,
        string? status = null,
        string? bookingType = null,
        string? category = null);

    /// <summary>
    /// Cancel a service request
    /// </summary>
    /// <param name="requestId">Service request ID</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <returns>Success indicator</returns>
    Task<(bool Success, string Message)> CancelServiceRequestAsync(
        Guid requestId,
        Guid userId);

    /// <summary>
    /// Get all service requests (admin only)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Records per page</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Paginated list of all service requests</returns>
    Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetAllServiceRequestsAsync(
        int page = 1,
        int pageSize = 10,
        string? status = null);
}
