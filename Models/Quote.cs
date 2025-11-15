using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Quote entity for provider quotes on service requests
/// </summary>
[Table("quotes")]
public class Quote
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [StringLength(1000)]
    public string? Message { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ProviderId))]
    public Provider? Provider { get; set; }

    [ForeignKey(nameof(RequestId))]
    public ServiceRequest? ServiceRequest { get; set; }
}