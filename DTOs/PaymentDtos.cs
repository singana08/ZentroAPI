using ZentroAPI.Models;

namespace ZentroAPI.DTOs;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreatePaymentRequest
{
    public Guid ServiceRequestId { get; set; }
    public Guid PayeeId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class CreatePaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string? Currency { get; set; } = "usd";
    public string? Description { get; set; }
}

public class PaymentIntentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
}