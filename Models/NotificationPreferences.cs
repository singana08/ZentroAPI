using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class NotificationPreferences
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public bool EnablePushNotifications { get; set; } = true;
    public bool NewRequests { get; set; } = true;
    public bool QuoteResponses { get; set; } = true;
    public bool StatusUpdates { get; set; } = true;
    public bool Messages { get; set; } = true;
    public bool Reminders { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}