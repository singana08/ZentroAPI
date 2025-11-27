using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.Models;

public class Agreement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid QuoteId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid ProviderId { get; set; }
    
    public bool RequesterAccepted { get; set; } = false;
    public bool ProviderAccepted { get; set; } = false;
    
    public DateTime? RequesterAcceptedAt { get; set; }
    public DateTime? ProviderAcceptedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    
    public AgreementStatus Status { get; set; } = AgreementStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AgreementStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3
}