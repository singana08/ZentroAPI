using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ZentroAPI.Services;

/// <summary>
/// Service implementation for managing service requests
/// </summary>
public class ServiceRequestService : IServiceRequestService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ServiceRequestService> _logger;
    private readonly INotificationService _notificationService;

    public ServiceRequestService(
        ApplicationDbContext dbContext,
        ILogger<ServiceRequestService> logger,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Create a new service request with validation based on booking type
    /// </summary>
    public async Task<(bool Success, string Message, ServiceRequestResponseDto? Data)> CreateServiceRequestAsync(
        Guid userId,
        CreateServiceRequestDto request)
    {
        try
        {
            // Validate requester exists
            var requesterExists = await _dbContext.Requesters.AnyAsync(r => r.Id == userId);
            if (!requesterExists)
            {
                _logger.LogWarning($"Requester profile {userId} not found for service request creation");
                return (false, "Requester profile not found", null);
            }

            // Validate booking type
            if (!Enum.TryParse<BookingType>(request.BookingType, ignoreCase: true, out var bookingType))
            {
                _logger.LogWarning($"Invalid booking type: {request.BookingType}");
                return (false, "Invalid booking type. Must be: book_now, schedule_later, or get_quote", null);
            }

            // Validate required common fields
            if (string.IsNullOrWhiteSpace(request.MainCategory))
                return (false, "Main category is required", null);

            if (string.IsNullOrWhiteSpace(request.SubCategory))
                return (false, "Sub-category is required", null);

            if (string.IsNullOrWhiteSpace(request.Location))
                return (false, "Location is required", null);

            // Validate booking-type-specific fields
            var validationResult = ValidateBookingTypeRequirements(bookingType, request);
            if (!validationResult.Valid)
                return (false, validationResult.Message, null);

            // Create service request
            var serviceRequest = new ServiceRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = userId,
                BookingType = bookingType,
                MainCategory = request.MainCategory.Trim(),
                SubCategory = request.SubCategory.Trim(),
                //Date = DateTime.Parse(request.Date),
                Date = (request.ScheduledDate ?? request.Date).HasValue ? DateTime.SpecifyKind((request.ScheduledDate ?? request.Date).Value, DateTimeKind.Utc) : null,
                Time = request.Time?.Trim(),
                Location = request.Location.Trim(),
                Latitude = request.Coordinates?.Latitude,
                Longitude = request.Coordinates?.Longitude,
                Notes = request.Notes?.Trim(),
                Title = request.Title?.Trim(),
                Description = request.Description?.Trim(),
                AdditionalNotes = request.AdditionalNotes?.Trim(),
                Status = ServiceRequestStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ServiceRequests.Add(serviceRequest);
            await _dbContext.SaveChangesAsync();

            // Notify all providers of new service request
            try
            {
                _logger.LogError($"DEBUG: About to call NotifyProvidersOfNewServiceRequestAsync for {serviceRequest.Id}");
                await _notificationService.NotifyProvidersOfNewServiceRequestAsync(serviceRequest.Id);
                _logger.LogError($"DEBUG: NotifyProvidersOfNewServiceRequestAsync completed for {serviceRequest.Id}");
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, $"NOTIFICATION FAILED for service request {serviceRequest.Id}: {notificationEx.Message}");
                // Don't fail the entire request creation if notifications fail
            }

            _logger.LogInformation(
                $"Service request created successfully. ID: {serviceRequest.Id}, Requester: {userId}, Type: {bookingType}");

            var responseDto = MapToResponseDto(serviceRequest, userId);
            return (true, "Service request created successfully", responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating service request: {ex.Message}", ex);
            return (false, "An error occurred while creating the service request", null);
        }
    }

    /// <summary>
    /// Update an existing service request
    /// </summary>
    public async Task<(bool Success, string Message, ServiceRequestResponseDto? Data)> UpdateServiceRequestAsync(
        Guid requestId,
        Guid userId,
        CreateServiceRequestDto request)
    {
        try
        {
            // Validate requester exists
            var requesterExists = await _dbContext.Requesters.AnyAsync(r => r.Id == userId);
            if (!requesterExists)
            {
                _logger.LogWarning($"Requester profile {userId} not found for service request update");
                return (false, "Requester profile not found", null);
            }

            var serviceRequest = await _dbContext.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.Id == requestId && sr.RequesterId == userId);

            if (serviceRequest == null)
            {
                _logger.LogWarning($"Service request {requestId} not found for requester {userId}");
                return (false, "Service request not found or you don't have permission to update it", null);
            }

            // Validate booking type
            if (!Enum.TryParse<BookingType>(request.BookingType, ignoreCase: true, out var bookingType))
            {
                _logger.LogWarning($"Invalid booking type: {request.BookingType}");
                return (false, "Invalid booking type. Must be: book_now, schedule_later, or get_quote", null);
            }

            // Validate required common fields
            if (string.IsNullOrWhiteSpace(request.MainCategory))
                return (false, "Main category is required", null);

            if (string.IsNullOrWhiteSpace(request.SubCategory))
                return (false, "Sub-category is required", null);

            if (string.IsNullOrWhiteSpace(request.Location))
                return (false, "Location is required", null);

            // Validate booking-type-specific fields
            var validationResult = ValidateBookingTypeRequirements(bookingType, request);
            if (!validationResult.Valid)
                return (false, validationResult.Message, null);

            // Update fields
            serviceRequest.BookingType = bookingType;
            serviceRequest.MainCategory = request.MainCategory.Trim();
            serviceRequest.SubCategory = request.SubCategory.Trim();
            serviceRequest.Date = request.ScheduledDate ?? request.Date;
            serviceRequest.Time = request.Time?.Trim();
            serviceRequest.Location = request.Location.Trim();
            serviceRequest.Latitude = request.Coordinates?.Latitude;
            serviceRequest.Longitude = request.Coordinates?.Longitude;
            serviceRequest.Notes = request.Notes?.Trim();
            serviceRequest.Title = request.Title?.Trim();
            serviceRequest.Description = request.Description?.Trim();
            serviceRequest.AdditionalNotes = request.AdditionalNotes?.Trim();
            serviceRequest.UpdatedAt = DateTime.UtcNow;

            // Status remains as is during updates

            _dbContext.ServiceRequests.Update(serviceRequest);
            await _dbContext.SaveChangesAsync();

            // Notify assigned provider of request update
            if (serviceRequest.AssignedProviderId.HasValue)
            {
                await _notificationService.NotifyProviderOfRequestUpdateAsync(serviceRequest.Id, "edit");
            }

            _logger.LogInformation(
                $"Service request updated successfully. ID: {serviceRequest.Id}, Requester: {userId}");

            var responseDto = MapToResponseDto(serviceRequest, userId);
            return (true, "Service request updated successfully", responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating service request: {ex.Message}", ex);
            return (false, "An error occurred while updating the service request", null);
        }
    }



    /// <summary>
    /// Get all service requests for a user with pagination and filtering
    /// </summary>
    public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetUserServiceRequestsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 10,
        string? status = null,
        string? bookingType = null,
        string? category = null)
    {
        try
        {
            // Validate requester exists
            var requesterExists = await _dbContext.Requesters.AsNoTracking().AnyAsync(r => r.Id == userId);
            if (!requesterExists)
            {
                _logger.LogWarning($"Requester profile {userId} not found for service requests retrieval");
                return (false, "Requester profile not found", null);
            }

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var query = _dbContext.ServiceRequests
                .Where(sr => sr.RequesterId == userId)
                .AsNoTracking();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ServiceRequestStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    query = query.Where(sr => sr.Status == parsedStatus);
                }
            }

            // Apply booking type filter if provided
            if (!string.IsNullOrWhiteSpace(bookingType))
            {
                if (Enum.TryParse<BookingType>(bookingType, ignoreCase: true, out var parsedBookingType))
                {
                    query = query.Where(sr => sr.BookingType == parsedBookingType);
                }
            }

            // Apply category filter if provided
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(sr => sr.MainCategory.Contains(category));
            }

            // Get total count
            var total = await query.CountAsync();

            // If no records found, return empty result
            if (total == 0)
            {
                var emptyResponse = new PaginatedServiceRequestsDto
                {
                    Data = new List<ServiceRequestResponseDto>(),
                    Total = 0,
                    Page = page,
                    PageSize = pageSize
                };
                return (true, "No service requests found", emptyResponse);
            }

            // Apply pagination
            var serviceRequests = await query
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseDtos = serviceRequests.Select(sr => MapToResponseDto(sr, userId)).ToList();

            var paginatedResponse = new PaginatedServiceRequestsDto
            {
                Data = responseDtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation(
                $"Retrieved {serviceRequests.Count} service requests for requester {userId}, page {page}");

            return (true, "Service requests retrieved successfully", paginatedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving service requests for user {userId}: {ex.Message}", ex);
            return (false, "An error occurred while retrieving service requests", null);
        }
    }

    /// <summary>
    /// Cancel a service request
    /// </summary>
    public async Task<(bool Success, string Message)> CancelServiceRequestAsync(
        Guid requestId,
        Guid userId)
    {
        try
        {
            // Validate requester exists
            var requesterExists = await _dbContext.Requesters.AnyAsync(r => r.Id == userId);
            if (!requesterExists)
            {
                _logger.LogWarning($"Requester profile {userId} not found for service request cancellation");
                return (false, "Requester profile not found");
            }

            var serviceRequest = await _dbContext.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.Id == requestId && sr.RequesterId == userId);

            if (serviceRequest == null)
            {
                _logger.LogWarning($"Service request {requestId} not found for requester {userId}");
                return (false, "Service request not found or you don't have permission to cancel it");
            }

            if (serviceRequest.Status == ServiceRequestStatus.Cancelled)
            {
                return (false, "Service request is already cancelled");
            }

            if (serviceRequest.Status == ServiceRequestStatus.Completed)
            {
                return (false, "Cannot cancel a completed service request");
            }

            serviceRequest.Status = ServiceRequestStatus.Cancelled;
            serviceRequest.UpdatedAt = DateTime.UtcNow;

            _dbContext.ServiceRequests.Update(serviceRequest);
            await _dbContext.SaveChangesAsync();

            // Notify assigned provider of request cancellation
            if (serviceRequest.AssignedProviderId.HasValue)
            {
                await _notificationService.NotifyProviderOfRequestUpdateAsync(serviceRequest.Id, "cancel");
            }

            _logger.LogInformation($"Service request {requestId} cancelled by requester {userId}");
            return (true, "Service request cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling service request {requestId}: {ex.Message}", ex);
            return (false, "An error occurred while cancelling the service request");
        }
    }

    /// <summary>
    /// Get all service requests (admin only)
    /// </summary>
    public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetAllServiceRequestsAsync(
        int page = 1,
        int pageSize = 10,
        string? status = null)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var query = _dbContext.ServiceRequests.AsNoTracking();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ServiceRequestStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    query = query.Where(sr => sr.Status == parsedStatus);
                }
            }

            // Get total count
            var total = await query.CountAsync();

            // Apply pagination
            var serviceRequests = await query
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseDtos = serviceRequests.Select(sr => MapToResponseDto(sr, null)).ToList();

            var paginatedResponse = new PaginatedServiceRequestsDto
            {
                Data = responseDtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation($"Retrieved all service requests, page {page}, total: {total}");
            return (true, "Service requests retrieved successfully", paginatedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving all service requests: {ex.Message}", ex);
            return (false, "An error occurred while retrieving service requests", null);
        }
    }

    /// <summary>
    /// Validate booking type specific requirements
    /// </summary>
    private static (bool Valid, string Message) ValidateBookingTypeRequirements(
        BookingType bookingType,
        CreateServiceRequestDto request)
    {
        return bookingType switch
        {
            BookingType.book_now => ValidateBookNow(request),
            BookingType.schedule_later => ValidateScheduleLater(request),
            BookingType.get_quote => ValidateGetQuote(request),
            _ => (false, "Invalid booking type")
        };
    }

    private static (bool Valid, string Message) ValidateBookNow(CreateServiceRequestDto request)
    {
        // For BookNow, date and time are not required
        // Only common required fields need to be validated  
        return (true, "");
    }

    private static (bool Valid, string Message) ValidateScheduleLater(CreateServiceRequestDto request)
    {
        var dateValue = request.ScheduledDate ?? request.Date;
        if (dateValue == null)
        {
            return (false, "Date is required for 'schedule_later' booking type");
        }

        if (string.IsNullOrWhiteSpace(request.Time))
        {
            return (false, "Time is required for 'schedule_later' booking type");
        }

        if (dateValue < DateTime.Now.Date.AddDays(1))
        {
            return (false, "Schedule date must be at least tomorrow");
        }

        return (true, "");
    }

    private static (bool Valid, string Message) ValidateGetQuote(CreateServiceRequestDto request)
    {
        // For get_quote, date and time are not required
        // Only common required fields need to be validated
        return (true, "");
    }

    /// <summary>
    /// Map ServiceRequest entity to ResponseDto
    /// </summary>
    private ServiceRequestResponseDto MapToResponseDto(ServiceRequest serviceRequest, Guid? currentUserId = null)
    {
        // Load quotes for this request - show quotes if user is requester OR provider
        var quotes = new List<QuoteResponseDto>();
        ReviewResponseDto? review = null;
        
        if (currentUserId.HasValue)
        {
            var isRequester = serviceRequest.RequesterId == currentUserId.Value;
            var isProvider = _dbContext.Providers.AsNoTracking().Any(p => p.Id == currentUserId.Value);
            
            if (isRequester || isProvider)
            {
                quotes = _dbContext.Quotes
                    .AsNoTracking()
                    .Include(q => q.Provider)
                    .ThenInclude(p => p!.User)
                    .Where(q => q.RequestId == serviceRequest.Id)
                    .GroupJoin(_dbContext.Agreements,
                        q => q.Id,
                        a => a.QuoteId,
                        (q, agreements) => new { Quote = q, Agreement = agreements.FirstOrDefault() })
                    .Select(qa => new QuoteResponseDto
                    {
                        Id = qa.Quote.Id,
                        ProviderId = qa.Quote.ProviderId,
                        RequestId = qa.Quote.RequestId,
                        Price = qa.Quote.Price,
                        Message = qa.Quote.Message,
                        CreatedAt = qa.Quote.CreatedAt,
                        ExpiresAt = qa.Quote.ExpiresAt,
                        QuoteStatus = qa.Quote.Status,
                        IsAcceptedByRequester = qa.Agreement != null && qa.Agreement.RequesterAccepted,
                        IsAcceptedByProvider = qa.Agreement != null && qa.Agreement.ProviderAccepted,
                        UpdatedAt = qa.Quote.UpdatedAt,
                        ProviderName = qa.Quote.Provider!.User!.FullName,
                        ProviderRating = qa.Quote.Provider.Rating
                    })
                    .ToListAsync()
                    .Result;

                // Load review if exists for this service request
                var reviewData = _dbContext.Reviews
                    .AsNoTracking()
                    .Include(r => r.Customer)
                    .ThenInclude(c => c!.User)
                    .FirstOrDefault(r => r.ServiceRequestId == serviceRequest.Id);

                if (reviewData != null)
                {
                    review = new ReviewResponseDto
                    {
                        Id = reviewData.Id,
                        CustomerName = reviewData.Customer?.User?.FullName ?? "Unknown",
                        ServiceType = $"{serviceRequest.MainCategory} - {serviceRequest.SubCategory}",
                        Rating = reviewData.Rating,
                        Comment = reviewData.Comment,
                        Date = reviewData.CreatedAt,
                        ServiceRequestId = reviewData.ServiceRequestId
                    };
                }
            }
        }

        // Get provider status if user is a provider
        string? providerStatus = null;
        if (currentUserId.HasValue && _dbContext.Providers.AsNoTracking().Any(p => p.Id == currentUserId.Value))
        {
            var status = _dbContext.ProviderRequestStatuses
                .AsNoTracking()
                .FirstOrDefault(prs => prs.ProviderId == currentUserId.Value && prs.RequestId == serviceRequest.Id);
            providerStatus = status?.Status.ToString() ?? "New";
        }

        // Get workflow status if provider is assigned
        WorkflowStatusResponseDto? workflowStatus = null;
        if (serviceRequest.AssignedProviderId.HasValue)
        {
            var workflow = _dbContext.WorkflowStatuses
                .AsNoTracking()
                .FirstOrDefault(ws => ws.RequestId == serviceRequest.Id && ws.ProviderId == serviceRequest.AssignedProviderId.Value);
            if (workflow != null)
            {
                workflowStatus = new WorkflowStatusResponseDto
                {
                    Id = workflow.Id,
                    RequestId = workflow.RequestId,
                    ProviderId = workflow.ProviderId,
                    IsAssigned = workflow.IsAssigned,
                    AssignedDate = workflow.AssignedDate,
                    IsInProgress = workflow.IsInProgress,
                    InProgressDate = workflow.InProgressDate,
                    IsCheckedIn = workflow.IsCheckedIn,
                    CheckedInDate = workflow.CheckedInDate,
                    IsCompleted = workflow.IsCompleted,
                    CompletedDate = workflow.CompletedDate,
                    CreatedAt = workflow.CreatedAt,
                    UpdatedAt = workflow.UpdatedAt
                };
            }
        }

        return new ServiceRequestResponseDto
        {
            Id = serviceRequest.Id,
            UserId = serviceRequest.RequesterId,
            BookingType = serviceRequest.BookingType.ToString(),
            MainCategory = serviceRequest.MainCategory,
            SubCategory = serviceRequest.SubCategory,
            Title = serviceRequest.Title,
            Description = serviceRequest.Description,
            Date = serviceRequest.Date,
            Time = serviceRequest.Time,
            Location = serviceRequest.Location,
            Notes = serviceRequest.Notes,
            AdditionalNotes = serviceRequest.AdditionalNotes,
            AssignedProviderId = serviceRequest.AssignedProviderId,
            RequestStatus = serviceRequest.Status.ToString(),
            ProviderStatus = providerStatus,
            QuoteCount = quotes.Count,
            Quotes = quotes,
            Review = review,
            WorkflowStatus = workflowStatus,
            CreatedAt = serviceRequest.CreatedAt,
            UpdatedAt = serviceRequest.UpdatedAt,
            Coordinates = serviceRequest.Latitude.HasValue && serviceRequest.Longitude.HasValue 
                ? new CoordinatesDto { Latitude = serviceRequest.Latitude.Value, Longitude = serviceRequest.Longitude.Value }
                : null
        };
    }

    /// <summary>
    /// PROVIDER: Get jobs I've quoted on or been assigned to (My Jobs)
    /// </summary>
    public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetProviderJobsAsync(
        Guid providerId, int page = 1, int pageSize = 10)
    {
        try
        {
            _logger.LogInformation($"Getting provider jobs for provider {providerId}");
            
            // Get requests where provider has quoted OR is assigned
            var quotedRequestIds = await _dbContext.Quotes
                .Where(q => q.ProviderId == providerId)
                .Select(q => q.RequestId)
                .ToListAsync();

            var baseQuery = _dbContext.ServiceRequests
                .Where(sr => quotedRequestIds.Contains(sr.Id) || sr.AssignedProviderId == providerId)
                .AsNoTracking();

            var total = await baseQuery.CountAsync();

            var serviceRequests = await baseQuery
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var requests = new List<ServiceRequestResponseDto>();
            foreach (var serviceRequest in serviceRequests)
            {
                var responseDto = MapToResponseDto(serviceRequest, providerId);
                requests.Add(responseDto);
            }

            var response = new PaginatedServiceRequestsDto
            {
                Data = requests,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return (true, "Provider jobs retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting provider jobs for provider {providerId}");
            return (false, "Failed to get provider jobs", null);
        }
    }

    /// <summary>
    /// PROVIDER: Get available jobs to quote on (Available Jobs)
    /// </summary>
    public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetAvailableJobsForProviderAsync(
        Guid providerId, int page = 1, int pageSize = 10)
    {
        try
        {
            _logger.LogInformation($"Getting available jobs for provider {providerId}");
            
            // Get hidden request IDs for this provider
            var hiddenRequestIds = await _dbContext.ProviderRequestStatuses
                .Where(prs => prs.ProviderId == providerId && prs.Status == ProviderStatus.Hidden)
                .Select(prs => prs.RequestId)
                .ToListAsync();

            // Get request IDs where this provider already has a quote
            var quotedRequestIds = await _dbContext.Quotes
                .Where(q => q.ProviderId == providerId)
                .Select(q => q.RequestId)
                .ToListAsync();

            // FIXED: Only show unassigned open/reopened requests OR requests assigned to current provider
            // Exclude requests assigned to OTHER providers and requests where provider already has a quote
            var baseQuery = _dbContext.ServiceRequests
                .Where(sr => (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.Reopened) 
                            && (sr.AssignedProviderId == null || sr.AssignedProviderId == providerId))
                .Where(sr => !quotedRequestIds.Contains(sr.Id) && !hiddenRequestIds.Contains(sr.Id))
                .AsNoTracking();

            var total = await baseQuery.CountAsync();

            var serviceRequests = await baseQuery
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var requests = new List<ServiceRequestResponseDto>();
            foreach (var serviceRequest in serviceRequests)
            {
                // Get provider status for this request
                var providerStatus = await _dbContext.ProviderRequestStatuses
                    .FirstOrDefaultAsync(prs => prs.ProviderId == providerId && prs.RequestId == serviceRequest.Id);

                // Get quote count (don't show actual quotes for available jobs)
                var quoteCount = await _dbContext.Quotes
                    .Where(q => q.RequestId == serviceRequest.Id)
                    .CountAsync();

                var responseDto = new ServiceRequestResponseDto
                {
                    Id = serviceRequest.Id,
                    UserId = serviceRequest.RequesterId,
                    BookingType = serviceRequest.BookingType.ToString(),
                    MainCategory = serviceRequest.MainCategory,
                    SubCategory = serviceRequest.SubCategory,
                    Title = serviceRequest.Title,
                    Description = serviceRequest.Description,
                    Date = serviceRequest.Date,
                    Time = serviceRequest.Time,
                    Location = serviceRequest.Location,
                    Notes = serviceRequest.Notes,
                    AdditionalNotes = serviceRequest.AdditionalNotes,
                    AssignedProviderId = serviceRequest.AssignedProviderId,
                    RequestStatus = serviceRequest.Status.ToString(),
                    ProviderStatus = providerStatus?.Status.ToString() ?? "New",
                    QuoteCount = quoteCount,
                    Quotes = new List<QuoteResponseDto>(), // Don't show quotes for available jobs
                    CreatedAt = serviceRequest.CreatedAt,
                    UpdatedAt = serviceRequest.UpdatedAt,
                    Coordinates = serviceRequest.Latitude.HasValue && serviceRequest.Longitude.HasValue 
                        ? new CoordinatesDto { Latitude = serviceRequest.Latitude.Value, Longitude = serviceRequest.Longitude.Value }
                        : null
                };

                requests.Add(responseDto);
            }

            var response = new PaginatedServiceRequestsDto
            {
                Data = requests,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return (true, "Available jobs retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting available jobs for provider {providerId}");
            return (false, "Failed to get available jobs", null);
        }
    }
}
