using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public enum TransactionType
{
    Credit,
    Debit
}

public enum TransactionSource
{
    ReferralBonus,
    ServicePayment,
    Withdrawal,
    Refund,
    Expiry
}

public class WalletTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid WalletId { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public TransactionSource Source { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public decimal BalanceAfter { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public Guid? ReferenceId { get; set; } // Links to referral, payment, etc.

    public DateTime? ExpiresAt { get; set; } // For referral credits

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Wallet Wallet { get; set; } = null!;
}