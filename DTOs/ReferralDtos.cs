using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.DTOs;

public class ReferralCodeDto
{
    public string ReferralCode { get; set; } = string.Empty;
    public int TotalReferrals { get; set; }
    public decimal TotalEarnings { get; set; }
}

public class UseReferralCodeRequest
{
    [Required]
    [StringLength(10)]
    public string ReferralCode { get; set; } = string.Empty;
}

public class ReferralStatsDto
{
    public string ReferralCode { get; set; } = string.Empty;
    public int TotalReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public int CompletedReferrals { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal PendingEarnings { get; set; }
    public List<ReferralDto> RecentReferrals { get; set; } = [];
}

public class ReferralDto
{
    public Guid Id { get; set; }
    public string ReferredUserName { get; set; } = string.Empty;
    public string ReferredUserEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BonusAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WalletDto
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public List<WalletTransactionDto> RecentTransactions { get; set; } = [];
}

public class WalletTransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}