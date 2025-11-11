using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HaluluAPI.Data;
using HaluluAPI.Models;
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
    private readonly IMessageService _messageService;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceRequestController> _logger;

    public ServiceRequestController(
        IServiceRequestService serviceRequestService,
        INotificationService notificationService,
        IMessageService messageService,
        ITokenService tokenService,
        ApplicationDbContext context,
        ILogger<ServiceRequestController> logger)
    {
        _serviceRequestService = serviceRequestService;
        _notificationService = notificationService;
        _messageService = messageService;
        _tokenService = tokenService;
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
                nameof(GetServiceRequests),
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
    /// Get all service requests - auto-detects requester vs provider from profile
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetServiceRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? bookingType = null,
        [FromQuery] string? category = null)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            // Check if profile is provider or requester
            var isProvider = await _context.Providers.AnyAsync(p => p.Id == profileId.Value);
            
            // Auto-detect based on profile type
            _logger.LogInformation($"Profile {profileId.Value} is provider: {isProvider}");
            
            var (listSuccess, listMessage, listData) = isProvider
                ? await _messageService.GetOpenRequestsAsync(profileId.Value, page, pageSize)
                : await _serviceRequestService.GetUserServiceRequestsAsync(profileId.Value, page, pageSize, status, bookingType, category);

            if (!listSuccess)
            {
                _logger.LogError($"Failed to get requests for profile {profileId.Value}, isProvider: {isProvider}, error: {listMessage}");
            }

            return listSuccess ? Ok(listData) : BadRequest(new ErrorResponse { Message = listMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service requests");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get single service request by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetServiceRequest([FromRoute] Guid id)    
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            var (success, message, data) = await _serviceRequestService.GetServiceRequestAsync(id, profileId.Value);
            return success ? Ok(data) : NotFound(new ErrorResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service request");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
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
    /// Hide a service request for the current provider
    /// </summary>
    [HttpPost("{id}/hide")]
    [Authorize]
    public async Task<IActionResult> HideServiceRequest([FromRoute] Guid id)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Success = false, Message = "Invalid profile authentication" });

            var providerExists = await _context.Providers.AnyAsync(p => p.Id == profileId.Value);
            if (!providerExists)
                return BadRequest(new ErrorResponse { Success = false, Message = "Provider profile not found" });

            var existingStatus = await _context.ProviderRequestStatuses
                .FirstOrDefaultAsync(prs => prs.ProviderId == profileId.Value && prs.RequestId == id);
            
            if (existingStatus == null)
            {
                var providerStatus = new ProviderRequestStatus
                {
                    ProviderId = profileId.Value,
                    RequestId = id,
                    Status = ProviderStatus.Hidden
                };
                _context.ProviderRequestStatuses.Add(providerStatus);
            }
            else
            {
                existingStatus.Status = ProviderStatus.Hidden;
                existingStatus.LastUpdated = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Request hidden successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding service request");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update provider status for a request
    /// </summary>
    [HttpPost("provider-status")]
    [Authorize]
    public async Task<IActionResult> UpdateProviderStatus([FromBody] UpdateProviderStatusDto request)
    {
        try
        {
            _logger.LogInformation($"UpdateProviderStatus called with RequestId: {request?.RequestId}, Status: {request?.Status}");
            
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                _logger.LogWarning("Profile authentication failed");
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });
            }

            _logger.LogInformation($"Provider ID: {profileId.Value}");
            
            var (success, message, data) = await _messageService.UpdateProviderStatusAsync(profileId.Value, request);
            
            _logger.LogInformation($"Service result - Success: {success}, Message: {message}");
            
            return success ? Ok(data) : BadRequest(new ErrorResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider status");
            return StatusCode(500, new ErrorResponse { Message = $"Internal server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get provider status for a request
    /// </summary>
    [HttpGet("provider-status")]
    [Authorize]
    public async Task<IActionResult> GetProviderStatus([FromQuery] Guid requestId)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            var (success, message, data) = await _messageService.GetProviderStatusAsync(profileId.Value, requestId);
            return success ? Ok(data) : NotFound(new ErrorResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider status");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign provider to request
    /// </summary>
    [HttpPost("assign")]
    [Authorize]
    public async Task<IActionResult> AssignProvider([FromBody] AssignProviderDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            var (success, message) = await _messageService.AssignProviderAsync(profileId.Value, request);
            return success ? Ok(new { Success = true, Message = message }) : BadRequest(new ErrorResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning provider");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reject request and reopen it
    /// </summary>
    [HttpPost("reject")]
    [Authorize]
    public async Task<IActionResult> RejectRequest([FromBody] RequestActionDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            var (success, message) = await _messageService.RejectRequestAsync(profileId.Value, request);
            return success ? Ok(new { Success = true, Message = message }) : BadRequest(new ErrorResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Complete request
    /// </summary>
    [HttpPost("complete")]
    [Authorize]
    public async Task<IActionResult> CompleteRequest([FromBody] RequestActionDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            var (success, message) = await _messageService.CompleteRequestAsync(profileId.Value, request);
            return success ? Ok(new { Success = true, Message = message }) : BadRequest(new ErrorResponse { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing request");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

}