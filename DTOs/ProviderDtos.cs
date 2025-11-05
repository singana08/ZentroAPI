using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for creating a provider profile
/// </summary>
public class CreateProviderDto
{
    [Required]
    [StringLength(1000)]
    public string Bio { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public int ExperienceYears { get; set; }

    [Required]
    public string ServiceCategories { get; set; } = string.Empty;

    public string? ServiceAreas { get; set; }

    [StringLength(100)]
    public string? PricingModel { get; set; }

    /// <summary>
    /// List of document/certification IDs
    /// </summary>
    public List<string>? DocumentIds { get; set; }

    /// <summary>
    /// Availability slots as JSON
    /// Example: {"Monday": {"start": "09:00", "end": "17:00"}, ...}
    /// </summary>
    public string? AvailabilitySlots { get; set; }
}

/// <summary>
/// DTO for updating provider profile
/// </summary>
public class UpdateProviderDto
{
    [StringLength(1000)]
    public string? Bio { get; set; }

    [Range(0, 100)]
    public int? ExperienceYears { get; set; }

    public string? ServiceCategories { get; set; }

    public string? ServiceAreas { get; set; }

    [StringLength(100)]
    public string? PricingModel { get; set; }

    public List<string>? DocumentIds { get; set; }

    public string? AvailabilitySlots { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for returning provider profile information
/// </summary>
public class ProviderDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Bio { get; set; }

    public int ExperienceYears { get; set; }

    public string? ServiceCategories { get; set; }

    public string? ServiceAreas { get; set; }

    public string? PricingModel { get; set; }

    public List<string>? DocumentIds { get; set; }

    public string? AvailabilitySlots { get; set; }

    public decimal Rating { get; set; }

    public decimal Earnings { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // User info
    public string? UserFullName { get; set; }

    public string? UserEmail { get; set; }

    public string? UserPhoneNumber { get; set; }

    public string? UserProfileImage { get; set; }
}

/// <summary>
/// DTO for provider profile with statistics
/// </summary>
public class ProviderProfileDto : ProviderDto
{
    public int TotalJobsCompleted { get; set; }

    public int ActiveJobs { get; set; }

    public int TotalReviews { get; set; }

    public decimal AverageResponseTime { get; set; }

    public decimal BidAcceptanceRate { get; set; }

    public bool IsVerified { get; set; }
}

/// <summary>
/// DTO for provider listing (for requester browsing)
/// </summary>
public class ProviderListDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? UserFullName { get; set; }

    public string? UserProfileImage { get; set; }

    public string? Bio { get; set; }

    public int ExperienceYears { get; set; }

    public decimal Rating { get; set; }

    public int TotalReviews { get; set; }

    public List<string>? ServiceCategories { get; set; }

    public string? PricingModel { get; set; }

    public bool IsAvailable { get; set; }
}

/// <summary>
/// DTO for availability slot configuration
/// </summary>
public class AvailabilitySlotDto
{
    public string DayOfWeek { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;

    public string? Notes { get; set; }
}