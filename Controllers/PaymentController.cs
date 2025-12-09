using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentroAPI.Services;
using Stripe;

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
        
        // Initialize Stripe with secret key from Key Vault
        var stripeSecretKey = _configuration["StripeSecretKey"];
        StripeConfiguration.ApiKey = stripeSecretKey;
        
        _logger.LogInformation($"Stripe initialized with key from Key Vault: {!string.IsNullOrEmpty(stripeSecretKey)}");
        
        _logger.LogInformation("Payment controller initialized with Stripe integration");
    }

    [HttpGet("config")]
    public IActionResult GetPaymentConfig()
    {
        var publishableKey = _configuration["StripePublishableKey"];
        return Ok(new { publishableKey });
    }

    [HttpPost("create-payment-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = "inr",
                Metadata = new Dictionary<string, string>
                {
                    { "job_id", request.JobId },
                    { "provider_id", request.ProviderId },
                    { "quote", request.Quote.ToString() },
                    { "platform_fee", request.PlatformFee.ToString() }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            
            _logger.LogInformation($"Stripe payment intent created: {paymentIntent.Id} for job {request.JobId}, amount {request.Amount}");
            
            return Ok(new { 
                clientSecret = paymentIntent.ClientSecret,
                client_secret = paymentIntent.ClientSecret, // For compatibility
                paymentIntentId = paymentIntent.Id,
                amount = request.Amount,
                currency = "inr"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment intent");
            return BadRequest(new { error = ex.Message });
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