using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Provider-specific status for service requests
/// </summary>
[Table("provider_request_status")]
public class ProviderRequestStatus
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public ProviderStatus Status { get; set; } = ProviderStatus.Viewed;

    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Guid? QuoteId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public ServiceRequest? ServiceRequest { get; set; }

    [ForeignKey(nameof(ProviderId))]
    public Provider? Provider { get; set; }

    [ForeignKey(nameof(QuoteId))]
    public Quote? Quote { get; set; }
}

/// <summary>
/// Provider-specific status enum
/// </summary>
public enum ProviderStatus
{
    Hidden = 0,
    Viewed = 1,
    Negotiating = 2,
    Quoted = 3,
    Assigned = 4,
    Rejected = 5,
    Completed = 6
}