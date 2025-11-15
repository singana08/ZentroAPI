using HaluluAPI.Models;

namespace HaluluAPI.DTOs;

/// <summary>
/// Request DTO for creating or updating service requests
/// Supports all three booking flows with unified structure.
/// 
/// Field Requirements by Booking Type:
/// - 'book_now': Date is REQUIRED (today or later), time optional
/// - 'schedule_later': Date is REQUIRED (tomorrow or later), time is REQUIRED
/// - 'get_quote': Date and time are OPTIONAL (can be completely omitted from payload)
/// </summary>
public class CreateServiceRequestDto
{
    /// <summary>
    /// Booking type: book_now, schedule_later, or get_quote
    /// </summary>
    public string BookingType { get; set; } = string.Empty;

    /// <summary>
    /// Main service category (e.g., "Cleaning Services")
    /// </summary>
    public string MainCategory { get; set; } = string.Empty;

    /// <summary>
    /// Sub-category under main category (e.g., "Deep Cleaning")
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// Service date - OPTIONAL. Only required when booking type is 'book_now' or 'schedule_later'.
    /// Can be omitted for 'get_quote' booking type.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Scheduled date (alternative field name for API compatibility)
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Preferred time in HH:mm format.
    /// - REQUIRED for 'schedule_later' booking type
    /// - OPTIONAL for 'book_now' booking type
    /// - Can be omitted for 'get_quote' booking type.
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Service location/address
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the service request
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Request title (optional)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Detailed description of the request
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Additional notes (max 500 characters)
    /// </summary>
    public string? AdditionalNotes { get; set; }

    /// <summary>
    /// Location coordinates
    /// </summary>
    public CoordinatesDto? Coordinates { get; set; }
}

/// <summary>
/// Request DTO for "Book Now" flow
/// Requires immediate booking details
/// </summary>
public class BookNowRequestDto
{
    /// <summary>
    /// Main service category
    /// </summary>
    public string MainCategory { get; set; } = string.Empty;

    /// <summary>
    /// Sub-category
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// Service date (required for book_now)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Preferred time (HH:mm format, optional)
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Service location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Service details
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO for "Schedule Later" flow
/// For future bookings
/// </summary>
public class ScheduleLaterRequestDto
{
    /// <summary>
    /// Main service category
    /// </summary>
    public string MainCategory { get; set; } = string.Empty;

    /// <summary>
    /// Sub-category
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// Preferred service date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Preferred time (HH:mm format, REQUIRED for schedule_later)
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Service location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Service details
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Additional requirements
    /// </summary>
    public string? AdditionalNotes { get; set; }
}

/// <summary>
/// Request DTO for "Get Quote" flow
/// For quote requests without fixed date/time
/// </summary>
public class GetQuoteRequestDto
{
    /// <summary>
    /// Main service category
    /// </summary>
    public string MainCategory { get; set; } = string.Empty;

    /// <summary>
    /// Sub-category
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// Service location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Detailed requirements for quote
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Additional specifications (max 500 chars)
    /// </summary>
    public string? AdditionalNotes { get; set; }
}

/// <summary>
/// Response DTO for service request
/// </summary>
public class ServiceRequestResponseDto
{
    /// <summary>
    /// Service request ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID who created the request
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Booking type (book_now, schedule_later, get_quote)
    /// </summary>
    public string BookingType { get; set; } = string.Empty;

    /// <summary>
    /// Main service category
    /// </summary>
    public string MainCategory { get; set; } = string.Empty;

    /// <summary>
    /// Sub-category
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// Service date (if applicable)
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Preferred time (if applicable)
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Service location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Service notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Request title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Request description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? AdditionalNotes { get; set; }

    /// <summary>
    /// Assigned provider ID (if assigned)
    /// </summary>
    public Guid? AssignedProviderId { get; set; }

    /// <summary>
    /// Assigned provider name (if assigned)
    /// </summary>
    public string? AssignedProviderName { get; set; }

    /// <summary>
    /// Global service request status (Open, Assigned, Completed, etc.)
    /// </summary>
    public string RequestStatus { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific status (Hidden, Viewed, Quoted, etc.) - only for providers
    /// </summary>
    public string? ProviderStatus { get; set; }

    /// <summary>
    /// Number of quotes received
    /// </summary>
    public int QuoteCount { get; set; }

    /// <summary>
    /// List of quotes for this request
    /// </summary>
    public List<QuoteResponseDto> Quotes { get; set; } = new();

    /// <summary>
    /// Number of unread messages
    /// </summary>
    public int UnreadMessageCount { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (nullable)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Location coordinates
    /// </summary>
    public CoordinatesDto? Coordinates { get; set; }
}

/// <summary>
/// Coordinates DTO for location
/// </summary>
public class CoordinatesDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>
/// Paginated response wrapper for service requests
/// </summary>
public class PaginatedServiceRequestsDto
{
    /// <summary>
    /// List of service requests
    /// </summary>
    public List<ServiceRequestResponseDto> Data { get; set; } = [];

    /// <summary>
    /// Total count of records
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (Total + PageSize - 1) / PageSize;
}