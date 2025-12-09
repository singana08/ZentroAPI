using Stripe;

namespace ZentroAPI.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
    {
        _logger = logger;
        
        // Try Key Vault first, then fallback to appsettings
        var stripeSecretKey = configuration["StripeSecretKey"] 
            ?? configuration["Stripe:SecretKey"];
            
        if (string.IsNullOrEmpty(stripeSecretKey))
        {
            _logger.LogError("Stripe secret key not found in configuration");
            throw new InvalidOperationException("Stripe secret key is required");
        }
        
        StripeConfiguration.ApiKey = stripeSecretKey;
        _logger.LogInformation("Stripe configuration initialized successfully");
    }

    public async Task<(bool Success, string Message, string? PaymentIntentId, string? ClientSecret)> CreatePaymentIntentAsync(
        decimal amount, string currency = "usd", string? customerId = null)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency,
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Stripe PaymentIntent created: {PaymentIntentId}", paymentIntent.Id);

            return (true, "Payment intent created successfully", paymentIntent.Id, paymentIntent.ClientSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent");
            return (false, $"Payment error: {ex.Message}", null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return (false, "Payment processing error", null, null);
        }
    }

    public async Task<(bool Success, string Message)> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId);

            if (paymentIntent.Status == "succeeded")
            {
                _logger.LogInformation("Payment confirmed successfully: {PaymentIntentId}", paymentIntentId);
                return (true, "Payment confirmed successfully");
            }

            return (false, $"Payment status: {paymentIntent.Status}");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming payment: {PaymentIntentId}", paymentIntentId);
            return (false, $"Payment confirmation failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RefundPaymentAsync(string paymentIntentId, decimal? amount = null)
    {
        try
        {
            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100); // Convert to cents
            }

            var refund = await refundService.CreateAsync(options);

            _logger.LogInformation("Refund created: {RefundId} for PaymentIntent: {PaymentIntentId}", 
                refund.Id, paymentIntentId);

            return (true, "Refund processed successfully");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error processing refund: {PaymentIntentId}", paymentIntentId);
            return (false, $"Refund failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, object? PaymentDetails)> GetPaymentStatusAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            var details = new
            {
                Id = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount / 100m, // Convert from cents
                Currency = paymentIntent.Currency,
                Created = paymentIntent.Created,
                Description = paymentIntent.Description
            };

            return (true, "Payment details retrieved", details);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting payment status: {PaymentIntentId}", paymentIntentId);
            return (false, $"Error retrieving payment: {ex.Message}", null);
        }
    }
}