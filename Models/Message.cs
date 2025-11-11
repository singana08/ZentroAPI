using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Message entity for chat between requesters and providers
/// </summary>
[Table("messages")]
public class Message
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SenderId { get; set; }

    [Required]
    public Guid ReceiverId { get; set; }

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    [StringLength(2000)]
    public string MessageText { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    // Navigation property
    [ForeignKey(nameof(RequestId))]
    public ServiceRequest? ServiceRequest { get; set; }
}