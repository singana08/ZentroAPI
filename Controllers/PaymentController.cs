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
            
            // Update transaction status
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == paymentIntentId);
                
            if (transaction != null)
            {
                transaction.Status = paymentIntent.Status;
                transaction.ErrorMessage = paymentIntent.LastPaymentError?.Message;
                transaction.UpdatedAt = DateTime.UtcNow;
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

    [HttpPost("confirm-payment")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentConfirmOptions
            {
                PaymentMethod = request.PaymentMethodId,
                ReturnUrl = request.ReturnUrl
            };
            
            var paymentIntent = await service.ConfirmAsync(request.PaymentIntentId, options);
            
            _logger.LogInformation($"Payment confirmation attempted: {paymentIntent.Id}, Status: {paymentIntent.Status}");
            
            return Ok(new {
                status = paymentIntent.Status,
                clientSecret = paymentIntent.ClientSecret,
                requiresAction = paymentIntent.Status == "requires_action",
                nextAction = paymentIntent.NextAction
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment");
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
                PaymentMethodTypes = new List<string> { "card", "upi" },
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
            
            // Log transaction
            var transaction = new Transaction
            {
                PaymentIntentId = paymentIntent.Id,
                UserId = userId ?? "unknown",
                JobId = request.JobId,
                ProviderId = request.ProviderId,
                Amount = request.Amount,
                Currency = "inr",
                Status = paymentIntent.Status,
                Quote = request.Quote,
                PlatformFee = request.PlatformFee,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Transaction logged: {paymentIntent.Id} for user {userId}");
            
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

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == request.PaymentIntentId);
                
            if (transaction == null)
            {
                _logger.LogWarning($"Transaction not found for payment intent {request.PaymentIntentId}");
                return NotFound(new { error = "Transaction not found" });
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(request.PaymentIntentId);

            if (paymentIntent.Status != "succeeded")
            {
                _logger.LogWarning($"Payment not succeeded. Status: {paymentIntent.Status}");
                return BadRequest(new { error = $"Payment status is {paymentIntent.Status}, expected succeeded" });
            }

            transaction.Status = "succeeded";
            transaction.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment processed successfully: {request.PaymentIntentId}");

            return Ok(new {
                success = true,
                message = "Payment processed successfully",
                transactionId = transaction.Id,
                paymentIntentId = request.PaymentIntentId,
                status = "succeeded",
                amount = transaction.Amount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var query = _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            
            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new {
                    t.Id,
                    t.PaymentIntentId,
                    t.JobId,
                    t.ProviderId,
                    t.Amount,
                    t.Currency,
                    t.Status,
                    t.Quote,
                    t.PlatformFee,
                    t.CreatedAt,
                    t.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation($"Retrieved {transactions.Count} payment history records for user {userId}");

            return Ok(new {
                success = true,
                data = transactions,
                pagination = new {
                    page,
                    pageSize,
                    total,
                    pages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history");
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

public class ConfirmPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class ProcessPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ServiceRequestId { get; set; } = string.Empty;
    public string PayeeId { get; set; } = string.Empty;
    public long Amount { get; set; }
}