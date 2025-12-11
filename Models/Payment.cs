using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid PayerId { get; set; }
    public Guid PayeeId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}

public enum PaymentMethod
{
    Card,
    CreditCard,
    DebitCard,
    UPI,
    NetBanking,
    Wallet,
    PayPal,
    BankTransfer
}