using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZentroAPI.Models;

/// <summary>
/// Provider profile - users who provide services
/// </summary>
[Table("providers")]
public class Provider
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    /// <summary>
    /// Service categories the provider offers as JSON array
    /// Example: ["plumbing", "electrical"]
    /// </summary>
    public string? ServiceCategories { get; set; }

    [Range(0, 100)]
    public int ExperienceYears { get; set; } = 0;

    [StringLength(1000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Service areas where provider operates as JSON array
    /// Example: ["downtown", "suburbs", "uptown"]
    /// </summary>
    public string? ServiceAreas { get; set; }

    [StringLength(100)]
    public string? PricingModel { get; set; }

    /// <summary>
    /// Document IDs/URLs as JSON array for certifications, licenses, etc.
    /// Example: ["doc_id_1", "doc_id_2"]
    /// </summary>
    public string? Documents { get; set; }

    /// <summary>
    /// Availability slots stored as JSONB
    /// Example: {
    ///   "Monday": {"start": "09:00", "end": "17:00", "available": true},
    ///   "Tuesday": {"start": "09:00", "end": "17:00", "available": true},
    ///   ...
    /// }
    /// </summary>
    public string? AvailabilitySlots { get; set; }

    [Range(0.0, 5.0)]
    [Column(TypeName = "numeric(3,2)")]
    public decimal Rating { get; set; } = 0;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Earnings { get; set; } = 0;

    [StringLength(200)]
    public string? PushToken { get; set; }

    public bool NotificationsEnabled { get; set; } = true;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public User? User { get; set; }
}