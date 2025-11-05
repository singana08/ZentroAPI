using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Service request entity representing customer booking requests
/// </summary>
public class ServiceRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RequesterId { get; set; }

    [Required]
    public BookingType BookingType { get; set; }

    [Required]
    [StringLength(100)]
    public string MainCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SubCategory { get; set; } = string.Empty;

    public DateTime? Date { get; set; }

    [StringLength(20)]
    public string? Time { get; set; }

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? AdditionalNotes { get; set; }

    [Required]
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(RequesterId))]
    public Requester? Requester { get; set; }
}

/// <summary>
/// Enum for booking types
/// </summary>
public enum BookingType
{
    book_now = 0,
    schedule_later = 1,
    get_quote = 2
}

/// <summary>
/// Enum for service request status
/// </summary>
public enum ServiceRequestStatus
{
    Pending = 0,
    QuoteRequested = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4
}