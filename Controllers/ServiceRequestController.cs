using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HaluluAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Controllers;

/// <summary>
/// Controller for managing service requests
/// Supports three booking flows: book_now, schedule_later, get_quote
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServiceRequestController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceRequestController> _logger;

    public ServiceRequestController(
        IServiceRequestService serviceRequestService,
        INotificationService notificationService,
        ApplicationDbContext context,
        ILogger<ServiceRequestController> logger)
    {
        _serviceRequestService = serviceRequestService;
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new service request
    /// Supports three booking types: book_now, schedule_later, get_quote
    /// </summary>
    /// <param name="request">Service request details with booking type</param>
    /// <returns>Created service request with generated ID</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ServiceRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateServiceRequest([FromBody] CreateServiceRequestDto request)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Request body cannot be empty"
                });
            }

            // Get current profile ID
            var profileIdClaim = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            {
                _logger.LogWarning("Invalid or missing profile ID in claims");
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Profile authentication failed"
                });
            }

            _logger.LogInformation($"Creating service request for profile {profileId}, booking type: {request.BookingType}");

            var (success, message, data) = await _serviceRequestService.CreateServiceRequestAsync(profileId, request);

            if (!success)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = message
                });
            }

            // Create notifications (don't block on failure)
            _ = Task.Run(async () =>
            {
                try
                {
                    // Notify requester (current profile)
                    await _notificationService.CreateNotificationAsync(
                        profileId,
                        "Service Request Created",
                        $"Your {data!.SubCategory} request has been submitted successfully",
                        "service_request_created",
                        data.Id,
                        new { serviceRequestId = data.Id, category = data.MainCategory, subCategory = data.SubCategory }
                    );

                    // Notify all provider profiles
                    var providerProfiles = await _context.Providers
                        .Where(p => p.IsActive)
                        .ToListAsync();
                    
                    foreach (var provider in providerProfiles)
                    {
                        await _notificationService.CreateNotificationAsync(
                            provider.Id,
                            "New Service Request Available",
                            $"{data.SubCategory} service needed in {data.Location}",
                            "new_service_request",
                            data.Id,
                            new { serviceRequestId = data.Id, category = data.MainCategory, subCategory = data.SubCategory, location = data.Location }
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create notifications for service request {data?.Id}");
                }
            });

            return CreatedAtAction(
                nameof(GetServiceRequest),
                new { id = data?.Id },
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating service request: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Update an existing service request
    /// Only the owner can update their own requests
    /// </summary>
    /// <param name="id">Service request ID</param>
    /// <param name="request">Updated service request details</param>
    /// <returns>Updated service request</returns>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateServiceRequest(
        [FromRoute] Guid id,
        [FromBody] CreateServiceRequestDto request)
    {
        try
        {
            // Validate ID
            if (id == Guid.Empty)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Invalid service request ID"
                });
            }

            // Validate request
            if (request == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Request body cannot be empty"
                });
            }

            // Get current profile ID
            var profileIdClaim = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            {
                _logger.LogWarning("Invalid or missing profile ID in claims");
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Profile authentication failed"
                });
            }

            _logger.LogInformation($"Updating service request {id} for profile {profileId}");

            var (success, message, data) = await _serviceRequestService.UpdateServiceRequestAsync(id, profileId, request);

            if (!success)
            {
                if (message.Contains("not found"))
                {
                    return NotFound(new ErrorResponse
                    {
                        Success = false,
                        Message = message
                    });
                }

                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating service request {id}: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Success = false,
                Message = "An error occurred while updating the service request"
            });
        }
    }

    /// <summary>
    /// Get a single service request by ID
    /// Only the owner can view their own requests
    /// </summary>
    /// <param name="id">Service request ID</param>
    /// <returns>Service request details</returns>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetServiceRequest([FromRoute] Guid id)
    {
        try
        {
            // Validate ID
            if (id == Guid.Empty)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Invalid service request ID"
                });
            }

            // Get current profile ID
            var profileIdClaim = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            {
                _logger.LogWarning("Invalid or missing profile ID in claims");
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Profile authentication failed"
                });
            }

            _logger.LogInformation($"Retrieving service request {id} for profile {profileId}");

            var (success, message, data) = await _serviceRequestService.GetServiceRequestAsync(id, profileId);

            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving service request {id}: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the service request"
            });
        }
    }

    /// <summary>
    /// Get all service requests for the current user with pagination and filtering
    /// User ID is automatically extracted from JWT token
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Records per page (default: 10, max: 100)</param>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="bookingType">Filter by booking type (optional)</param>
    /// <param name="category">Filter by main category (optional)</param>
    /// <returns>Paginated list of service requests</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedServiceRequestsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserServiceRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? bookingType = null,
        [FromQuery] string? category = null)
    {
        try
        {
            // Get current profile ID from JWT token
            var profileIdClaim = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            {
                _logger.LogWarning("Invalid or missing profile ID in claims");
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Profile authentication failed"
                });
            }

            _logger.LogInformation(
                $"Retrieving service requests for profile {profileId}, page: {page}, pageSize: {pageSize}");

            var (success, message, data) = await _serviceRequestService.GetUserServiceRequestsAsync(
                profileId,
                page,
                pageSize,
                status,
                bookingType,
                category);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving service requests: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Success = false,
                Message = "An error occurred while retrieving service requests"
            });
        }
    }

    /// <summary>
    /// Cancel a service request
    /// Only the owner can cancel their own requests
    /// </summary>
    /// <param name="id">Service request ID to cancel</param>
    /// <returns>Cancellation result</returns>
    [HttpDelete("{id}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelServiceRequest([FromRoute] Guid id)
    {
        try
        {
            // Validate ID
            if (id == Guid.Empty)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Invalid service request ID"
                });
            }

            // Get current profile ID
            var profileIdClaim = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(profileIdClaim) || !Guid.TryParse(profileIdClaim, out var profileId))
            {
                _logger.LogWarning("Invalid or missing profile ID in claims");
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Profile authentication failed"
                });
            }

            _logger.LogInformation($"Cancelling service request {id} for profile {profileId}");

            var (success, message) = await _serviceRequestService.CancelServiceRequestAsync(id, profileId);

            if (!success)
            {
                if (message.Contains("not found"))
                {
                    return NotFound(new ErrorResponse
                    {
                        Success = false,
                        Message = message
                    });
                }

                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(new
            {
                Success = true,
                Message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling service request {id}: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Success = false,
                Message = "An error occurred while cancelling the service request"
            });
        }
    }

    /// <summary>
    /// Get all service requests (admin only)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Records per page (default: 10, max: 100)</param>
    /// <param name="status">Filter by status (optional)</param>
    /// <returns>Paginated list of all service requests</returns>
    [HttpGet("admin/all")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedServiceRequestsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllServiceRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        try
        {
            _logger.LogInformation($"Retrieving all service requests, page: {page}, pageSize: {pageSize}");

            var (success, message, data) = await _serviceRequestService.GetAllServiceRequestsAsync(
                page,
                pageSize,
                status);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving all service requests: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Success = false,
                Message = "An error occurred while retrieving service requests"
            });
        }
    }
}