using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Hidden service requests for providers
/// </summary>
[Table("hidden_requests")]
public class HiddenRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public Guid ServiceRequestId { get; set; }

    [Required]
    public DateTime HiddenAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Provider? Provider { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
}