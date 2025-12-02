using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZentroAPI.Models;

/// <summary>
/// Review entity for provider ratings and feedback
/// </summary>
[Table("Reviews")]
public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceRequestId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ServiceRequestId))]
    public ServiceRequest? ServiceRequest { get; set; }

    [ForeignKey(nameof(ProviderId))]
    public Provider? Provider { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Requester? Customer { get; set; }
}