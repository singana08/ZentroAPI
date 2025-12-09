using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZentroAPI.Services;
using ZentroAPI.Data;
using ZentroAPI.Models;
using Stripe;
using System.Security.Claims;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;
    private readonly ApplicationDbContext _context;

    public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger, ApplicationDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        
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

    [HttpGet("status/{paymentIntentId}")]
    public async Task<IActionResult> GetPaymentStatus(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);
            
            // Update payment status
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
                
            if (payment != null)
            {
                payment.Status = paymentIntent.Status switch
                {
                    "succeeded" => PaymentStatus.Completed,
                    "processing" => PaymentStatus.Processing,
                    "requires_payment_method" => PaymentStatus.Failed,
                    "canceled" => PaymentStatus.Failed,
                    _ => PaymentStatus.Pending
                };
                
                if (payment.Status == PaymentStatus.Completed)
                {
                    payment.CompletedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
            }
            
            return Ok(new {
                status = paymentIntent.Status,
                amount = paymentIntent.Amount,
                currency = paymentIntent.Currency,
                lastPaymentError = paymentIntent.LastPaymentError?.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {PaymentIntentId}", paymentIntentId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("create-payment-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
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
                    { "platform_fee", request.PlatformFee.ToString() },
                    { "user_id", userId ?? "unknown" }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            
            // Save payment record to database
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = Guid.Parse(request.JobId),
                PayerId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                PayeeId = Guid.Parse(request.ProviderId),
                Amount = request.Amount / 100m, // Convert from cents to rupees
                Status = PaymentStatus.Pending,
                Method = Models.PaymentMethod.Card,
                TransactionId = paymentIntent.Id,
                PaymentIntentId = paymentIntent.Id,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Payment record saved: {payment.Id} for Stripe intent: {paymentIntent.Id}");
            
            return Ok(new { 
                clientSecret = paymentIntent.ClientSecret,
                client_secret = paymentIntent.ClientSecret,
                paymentIntentId = paymentIntent.Id,
                amount = request.Amount,
                currency = "inr"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
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