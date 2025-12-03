namespace ZentroAPI.Services;

public interface IStripePaymentService
{
    Task<(bool Success, string Message, string? PaymentIntentId, string? ClientSecret)> CreatePaymentIntentAsync(
        decimal amount, string currency = "usd", string? customerId = null);
    
    Task<(bool Success, string Message)> ConfirmPaymentAsync(string paymentIntentId);
    
    Task<(bool Success, string Message)> RefundPaymentAsync(string paymentIntentId, decimal? amount = null);
    
    Task<(bool Success, string Message, object? PaymentDetails)> GetPaymentStatusAsync(string paymentIntentId);
}