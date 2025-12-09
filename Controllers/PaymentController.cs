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
        
        // Initialize Stripe with secret key
        var stripeSecretKey = _configuration["StripeSecretKey"] ?? "sk_test_51SZb3FSMWgNWNSmya3XMIxRsDKA4E7SNpqKaSvBFHCBDwUNPPi6LBi5RiUkFXs7SuS32fiNdx5jqC1D2lpiCVjBs00iPRDvawS";
        StripeConfiguration.ApiKey = stripeSecretKey;
        
        _logger.LogInformation("Payment controller initialized with Stripe integration");
    }

    [HttpGet("config")]
    public IActionResult GetPaymentConfig()
    {
        try
        {
            var publishableKey = _configuration["StripePublishableKey"] ?? "pk_test_51SZb3FSMWgNWNSmyFlpUoBqHu1sZnLrq47lofvJMfE5Tc8jeRFRaHEPegMDMzUKphMBRyVpt3VVh1jds5Xr85GHq00DdteDhl6";
            
            return Ok(new { 
                publishableKey = publishableKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment config");
            return BadRequest(new { error = ex.Message });
        }
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