using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Provider performance metrics tracking
/// </summary>
[Table("ProviderMetrics")]
public class ProviderMetrics
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public int ResponseTimeMinutes { get; set; }

    public int JobsAssigned { get; set; }

    public int JobsCompleted { get; set; }

    public int JobsCancelled { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ProviderId))]
    public Provider? Provider { get; set; }
}