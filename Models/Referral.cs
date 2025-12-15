using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public enum ReferralStatus
{
    Pending,
    Completed,
    Expired
}

public class Referral
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ReferrerId { get; set; }

    [Required]
    public Guid ReferredUserId { get; set; }

    [Required]
    [StringLength(10)]
    public string ReferralCode { get; set; } = string.Empty;

    [Required]
    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;

    [Required]
    public decimal BonusAmount { get; set; } = 50; // Fixed ₹50

    public DateTime? CompletedAt { get; set; }

    public Guid? WalletTransactionId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Referrer { get; set; } = null!;
    public User ReferredUser { get; set; } = null!;
    public WalletTransaction? WalletTransaction { get; set; }
}