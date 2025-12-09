using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using ZentroAPI.Services;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _logger.LogInformation("Payment controller initialized with mock payment processing");
    }

    [HttpPost("create-payment-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            // Mock payment intent for testing
            var mockClientSecret = $"pi_mock_{Guid.NewGuid():N}_secret_mock";
            
            _logger.LogInformation($"Mock payment intent created for job {request.JobId}, amount {request.Amount}");
            
            return Ok(new { client_secret = mockClientSecret });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret
            );

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentSuccess(paymentIntent);
                    break;

                case Events.PaymentIntentPaymentFailed:
                    var failedPayment = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentFailure(failedPayment);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook error");
            return BadRequest();
        }
    }

    private async Task HandlePaymentSuccess(PaymentIntent paymentIntent)
    {
        var jobId = paymentIntent.Metadata["job_id"];
        var providerId = paymentIntent.Metadata["provider_id"];
        var quote = decimal.Parse(paymentIntent.Metadata["quote"]);
        var commission = quote * 0.10m;
        var providerPayout = quote - commission;

        // Transfer funds to provider
        await TransferToProvider(providerId, (long)(providerPayout * 100), jobId);
        
        // Update booking status
        // TODO: Update booking status in database
        
        _logger.LogInformation($"Payment succeeded for job {jobId}, transferred ₹{providerPayout} to provider {providerId}");
    }

    private async Task HandlePaymentFailure(PaymentIntent paymentIntent)
    {
        var jobId = paymentIntent.Metadata["job_id"];
        _logger.LogWarning($"Payment failed for job {jobId}");
        
        // TODO: Update booking status and notify users
    }

    private async Task TransferToProvider(string providerId, long amount, string jobId)
    {
        try
        {
            var options = new TransferCreateOptions
            {
                Amount = amount,
                Currency = "inr",
                Destination = $"acct_{providerId}", // Provider's Stripe Connect account
                Metadata = new Dictionary<string, string>
                {
                    ["job_id"] = jobId,
                    ["type"] = "service_payment"
                }
            };

            var service = new TransferService();
            await service.CreateAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to transfer funds to provider {providerId}");
            throw;
        }
    }
}

public class CreatePaymentIntentRequest
{
    public long Amount { get; set; }
    public string JobId { get; set; }
    public string ProviderId { get; set; }
    public decimal Quote { get; set; }
    public decimal PlatformFee { get; set; }
}