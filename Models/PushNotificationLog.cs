using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class PushNotificationLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Body { get; set; } = string.Empty;

    public string? Data { get; set; } // JSON string

    [StringLength(50)]
    public string Status { get; set; } = "sent"; // 'sent', 'failed', 'delivered'

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    // Navigation property
    public User? User { get; set; }
}