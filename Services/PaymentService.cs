using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IStripePaymentService _stripeService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ApplicationDbContext context, IStripePaymentService stripeService, ILogger<PaymentService> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? PaymentIntentId)> CreatePaymentIntentAsync(
        decimal amount, string currency = "usd")
    {
        var result = await _stripeService.CreatePaymentIntentAsync(amount, currency);
        return (result.Success, result.Message, result.PaymentIntentId);
    }

    public async Task<(bool Success, string Message)> ProcessPaymentAsync(
        Guid serviceRequestId, Guid payerId, Guid payeeId, decimal amount, string paymentIntentId)
    {
        try
        {
            // Confirm payment with Stripe
            var stripeResult = await _stripeService.ConfirmPaymentAsync(paymentIntentId);
            
            if (!stripeResult.Success)
            {
                return (false, stripeResult.Message);
            }

            // Save payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = serviceRequestId,
                PayerId = payerId,
                PayeeId = payeeId,
                Amount = amount,
                Status = PaymentStatus.Completed,
                Method = PaymentMethod.CreditCard,
                PaymentIntentId = paymentIntentId,
                TransactionId = paymentIntentId,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment processed successfully: {PaymentId} with Stripe: {PaymentIntentId}", 
                payment.Id, paymentIntentId);
            
            return (true, "Payment processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment with Stripe");
            return (false, "Payment processing failed");
        }
    }

    public async Task<(bool Success, string Message)> RefundPaymentAsync(Guid paymentId)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                return (false, "Payment not found");

            if (string.IsNullOrEmpty(payment.PaymentIntentId))
                return (false, "Payment intent ID not found");

            // Process refund with Stripe
            var stripeResult = await _stripeService.RefundPaymentAsync(payment.PaymentIntentId);
            
            if (!stripeResult.Success)
            {
                return (false, stripeResult.Message);
            }

            // Update payment status
            payment.Status = PaymentStatus.Refunded;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment refunded successfully: {PaymentId}", paymentId);
            return (true, "Payment refunded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment");
            return (false, "Refund failed");
        }
    }

    public async Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId)
    {
        return await _context.Payments
            .Where(p => p.PayerId == userId || p.PayeeId == userId)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                ServiceRequestId = p.ServiceRequestId,
                Amount = p.Amount,
                Status = p.Status,
                Method = p.Method,
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}