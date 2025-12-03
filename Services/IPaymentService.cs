using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface IPaymentService
{
    Task<(bool Success, string Message, string? PaymentIntentId)> CreatePaymentIntentAsync(
        decimal amount, string currency = "usd");
    
    Task<(bool Success, string Message)> ProcessPaymentAsync(
        Guid serviceRequestId, Guid payerId, Guid payeeId, decimal amount, string paymentMethodId);
    
    Task<(bool Success, string Message)> RefundPaymentAsync(Guid paymentId);
    
    Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId);
}