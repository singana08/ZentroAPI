using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class UserPushToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string PushToken { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DeviceType { get; set; } = string.Empty; // 'ios' or 'android'

    [StringLength(255)]
    public string? DeviceId { get; set; }

    [StringLength(50)]
    public string? AppVersion { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}