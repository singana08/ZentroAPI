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
    private readonly IWorkflowService _workflowService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceRequestController> _logger;

    public ServiceRequestController(
        IServiceRequestService serviceRequestService,
        INotificationService notificationService,
        IMessageService messageService,
        ITokenService tokenService,
        IWorkflowService workflowService,
        ApplicationDbContext context,
        ILogger<ServiceRequestController> logger)
    {
        _serviceRequestService = serviceRequestService;
        _notificationService = notificationService;
        _messageService = messageService;
        _tokenService = tokenService;
        _workflowService = workflowService;
        _context = context;
        _logger = logger;
    }

    // REQUESTER ENDPOINTS
    
    /// <summary>
    /// Create a new service request (Requester only)
    /// Supports three booking types: book_now, schedule_later, get_quote
    /// Validates booking-specific requirements and creates request with Open status
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDto request)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message, data) = await _serviceRequestService.CreateServiceRequestAsync(profileId.Value, request);
        if (!success)
            return BadRequest(new ErrorResponse { Message = message });

        return CreatedAtAction(nameof(Get), new { id = data?.Id }, data);
    }

    /// <summary>
    /// Update an existing service request (Requester only)
    /// Only the request owner can update their own requests
    /// Validates all fields and maintains existing status
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CreateServiceRequestDto request)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message, data) = await _serviceRequestService.UpdateServiceRequestAsync(id, profileId.Value, request);
        if (!success)
            return message.Contains("not found") ? NotFound(new ErrorResponse { Message = message }) : BadRequest(new ErrorResponse { Message = message });

        return Ok(data);
    }

    /// <summary>
    /// Get requester's own service requests with filtering and pagination
    /// Supports filtering by status, booking type, and category
    /// Returns paginated list with quotes, reviews, and request details
    /// </summary>
    [HttpGet("requester")]
    [Authorize]
    public async Task<IActionResult> Requester([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, [FromQuery] string? bookingType = null, [FromQuery] string? category = null)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message, data) = await _serviceRequestService.GetUserServiceRequestsAsync(profileId.Value, page, pageSize, status, bookingType, category);
        return success ? Ok(data) : BadRequest(new ErrorResponse { Message = message });
    }

    /// <summary>
    /// Cancel a service request (Requester only)
    /// Only the request owner can cancel their own requests
    /// Changes status to Cancelled and prevents further provider interactions
    /// </summary>
    [HttpDelete("{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message) = await _serviceRequestService.CancelServiceRequestAsync(id, profileId.Value);
        if (!success)
            return message.Contains("not found") ? NotFound(new ErrorResponse { Message = message }) : BadRequest(new ErrorResponse { Message = message });

        return Ok(new { Success = true, Message = message });
    }

    // SHARED ENDPOINTS
    
    /// <summary>
    /// Get service requests with auto-detection of user type
    /// Routes to provider jobs or requester requests based on profile
    /// Maintained for backward compatibility
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, [FromQuery] string? bookingType = null, [FromQuery] string? category = null)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var isProvider = await _context.Providers.AnyAsync(p => p.Id == profileId.Value);
        var (success, message, data) = isProvider
            ? await _messageService.GetProviderJobsAsync(profileId.Value, page, pageSize)
            : await _serviceRequestService.GetUserServiceRequestsAsync(profileId.Value, page, pageSize, status, bookingType, category);

        return success ? Ok(data) : BadRequest(new ErrorResponse { Message = message });
    }

    // PROVIDER ENDPOINTS
    
    /// <summary>
    /// Get provider's jobs - requests they've quoted on or been assigned to
    /// Shows provider's active pipeline including quoted and assigned requests
    /// Used for "My Jobs" section in provider dashboard
    /// </summary>
    [HttpGet("provider")]
    [Authorize]
    public async Task<IActionResult> Provider([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message, data) = await _messageService.GetProviderJobsAsync(profileId.Value, page, pageSize);
        return success ? Ok(data) : BadRequest(new ErrorResponse { Message = message });
    }

    /// <summary>
    /// Get available service requests for providers to quote on
    /// Shows open/reopened requests excluding hidden ones
    /// Includes requests assigned to current provider for ongoing work
    /// </summary>
    [HttpGet("provider/available")]
    [Authorize]
    public async Task<IActionResult> Available([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message, data) = await _messageService.GetOpenRequestsAsync(profileId.Value, page, pageSize);
        return success ? Ok(data) : BadRequest(new ErrorResponse { Message = message });
    }

    /// <summary>
    /// Hide a service request from provider's available list
    /// Allows providers to remove unwanted requests from their view
    /// Sets provider status to Hidden for this specific request
    /// </summary>
    [HttpPost("{id}/hide")]
    [Authorize]
    public async Task<IActionResult> Hide([FromRoute] Guid id)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var providerExists = await _context.Providers.AnyAsync(p => p.Id == profileId.Value);
        if (!providerExists)
            return BadRequest(new ErrorResponse { Message = "Provider profile not found" });

        var existingStatus = await _context.ProviderRequestStatuses
            .FirstOrDefaultAsync(prs => prs.ProviderId == profileId.Value && prs.RequestId == id);
        
        if (existingStatus == null)
        {
            _context.ProviderRequestStatuses.Add(new ProviderRequestStatus
            {
                ProviderId = profileId.Value,
                RequestId = id,
                Status = ProviderStatus.Hidden
            });
        }
        else
        {
            existingStatus.Status = ProviderStatus.Hidden;
            existingStatus.LastUpdated = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        return Ok(new { Success = true, Message = "Request hidden successfully" });
    }

    /// <summary>
    /// Get detailed service request with quotes and conversations
    /// Returns request info, quotes grouped by provider with their conversations,
    /// general messages, and review data for comprehensive view
    /// </summary>
    [HttpGet("{id:guid}/details")]
    [Authorize]
    public async Task<IActionResult> Details([FromRoute] Guid id)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            var serviceRequest = await _context.ServiceRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (serviceRequest == null)
                return NotFound(new ErrorResponse { Message = "Service request not found" });

            // Get quotes
            var quotes = await _context.Quotes
                .Include(q => q.Provider)
                .ThenInclude(p => p!.User)
                .Where(q => q.RequestId == id)
                .ToListAsync();

            // Get all messages for this request
            var allMessages = await _context.Messages
                .Where(m => m.RequestId == id)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            // Build quotes with messages
            var quotesWithMessages = quotes.Select(q => new QuoteWithMessagesDto
            {
                Id = q.Id,
                ProviderId = q.ProviderId,
                ProviderName = q.Provider!.User!.FullName,
                Price = q.Price,
                Message = q.Message,
                CreatedAt = q.CreatedAt,
                IsAcceptedByRequester = _context.Agreements.Any(a => a.QuoteId == q.Id && a.RequesterAccepted),
                Messages = allMessages
                    .Where(m => (m.SenderId == serviceRequest.RequesterId && m.ReceiverId == q.ProviderId) ||
                               (m.SenderId == q.ProviderId && m.ReceiverId == serviceRequest.RequesterId))
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        MessageText = m.MessageText,
                        IsFromProvider = m.SenderId == q.ProviderId,
                        IsFromRequester = m.SenderId == serviceRequest.RequesterId,
                        SentAt = m.Timestamp
                    })
                    .ToList()
            }).ToList();

            // Get provider IDs that have quotes
            var providerIdsWithQuotes = quotes.Select(q => q.ProviderId).ToHashSet();

            // Get general messages (not tied to providers with quotes)
            var generalMessages = allMessages
                .Where(m => !providerIdsWithQuotes.Contains(m.SenderId) && !providerIdsWithQuotes.Contains(m.ReceiverId))
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    MessageText = m.MessageText,
                    IsFromProvider = _context.Providers.Any(p => p.Id == m.SenderId),
                    IsFromRequester = m.SenderId == serviceRequest.RequesterId,
                    SentAt = m.Timestamp
                })
                .ToList();

            // Get review if exists
            var review = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ServiceRequestId == id)
                .Select(r => new ReviewDto
                {
                    Rating = r.Rating,
                    Comment = r.Comment
                })
                .FirstOrDefaultAsync();

            var response = new ServiceRequestDetailsDto
            {
                Id = serviceRequest.Id,
                RequestStatus = serviceRequest.Status.ToString(),
                MainCategory = serviceRequest.MainCategory,
                SubCategory = serviceRequest.SubCategory,
                BookingType = serviceRequest.BookingType.ToString(),
                Location = serviceRequest.Location,
                Notes = serviceRequest.Notes,
                CreatedAt = serviceRequest.CreatedAt,
                Quotes = quotesWithMessages,
                Messages = generalMessages,
                Review = review
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service request details");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }


    /// <summary>
    /// Update provider's status for a specific request
    /// </summary>
    [HttpPost("provider-status")]
    [Authorize]
    public async Task<IActionResult> UpdateProviderStatus([FromBody] UpdateProviderStatusDto request)
    {
        var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
        if (!profileId.HasValue)
            return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

        var (success, message, data) = await _messageService.UpdateProviderStatusAsync(profileId.Value, request);
        return success ? Ok(data) : BadRequest(new ErrorResponse { Message = message });
    }

    /// <summary>
    /// Update workflow status for a service request
    /// Only assigned providers can update workflow status
    /// </summary>
    /// <param name="id">Service request ID</param>
    /// <param name="request">Workflow status update</param>
    /// <returns>Updated workflow status</returns>
    [HttpPut("{id}/workflow-status")]
    [Authorize]
    [ProducesResponseType(typeof(WorkflowStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> WorkflowStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateWorkflowStatusDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile authentication failed" });

            // Verify user is a provider
            var isProvider = await _context.Providers.AnyAsync(p => p.Id == profileId.Value);
            if (!isProvider)
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Only providers can update workflow status"
                });
            }

            var (success, message, data) = await _workflowService.UpdateWorkflowStatusAsync(id, profileId.Value, request.Status);

            if (!success)
            {
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
            _logger.LogError(ex, "Error updating workflow status for request {RequestId}", id);
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }





}