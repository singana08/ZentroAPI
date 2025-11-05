using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

public class OtpRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string OtpCode { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int AttemptCount { get; set; } = 0;

    public int MaxAttempts { get; set; } = 5;

    public bool IsLocked { get; set; } = false;

    // Foreign key
    public Guid? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public OtpPurpose Purpose { get; set; } = OtpPurpose.Authentication;
}

public enum OtpPurpose
{
    Authentication = 0,
    PhoneVerification = 1,
    PasswordReset = 2
}