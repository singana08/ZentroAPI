using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZentroAPI.Services;
using ZentroAPI.Data;
using ZentroAPI.Models;
using Stripe;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

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
        _logger.LogInformation("Payment controller initialized with Stripe and Cashfree integration");
    }

    [HttpGet("config")]
    public IActionResult GetPaymentConfig()
    {
        var publishableKey = _configuration["StripePublishableKey"];
        var cashfreeAppId = _configuration["CashFreeAPPID"];
        
        return Ok(new { 
            publishableKey = publishableKey,
            cashfreeAppId = cashfreeAppId,
            environment = "sandbox"
        });
    }
    
    [HttpGet("test-cashfree")]
    public IActionResult TestCashfreeConfig()
    {
        var appId = _configuration["CashFreeAPPID"];
        var secretKey = _configuration["cashfreesecretkey"];
        
        return Ok(new {
            hasAppId = !string.IsNullOrEmpty(appId),
            hasSecretKey = !string.IsNullOrEmpty(secretKey),
            appIdLength = appId?.Length ?? 0,
            secretKeyLength = secretKey?.Length ?? 0
        });
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
                lastPaymentError = paymentIntent.LastPaymentError?.Message,
                requiresAction = paymentIntent.Status == "requires_action",
                nextAction = paymentIntent.NextAction,
                clientSecret = paymentIntent.ClientSecret
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {PaymentIntentId}", paymentIntentId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback([FromQuery] string payment_intent, [FromQuery] string payment_intent_client_secret)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(payment_intent);
            
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == payment_intent);
                
            if (transaction != null)
            {
                transaction.Status = paymentIntent.Status;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            
            // Redirect to mobile app deep link
            var deepLink = $"zentroapp://payment/callback?payment_intent={payment_intent}&status={paymentIntent.Status}";
            return Redirect(deepLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payment callback");
            return Redirect($"zentroapp://payment/callback?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpPost("confirm-payment")]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var returnUrl = $"{baseUrl}/api/payment/callback";
            
            var service = new PaymentIntentService();
            var options = new PaymentIntentConfirmOptions
            {
                PaymentMethod = request.PaymentMethodId,
                ReturnUrl = returnUrl
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

    // Cashfree Endpoints
    [HttpPost("cashfree/create-payment")]
    public async Task<IActionResult> CreateCashfreePayment([FromBody] CreatePaymentIntentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
        try
        {
            _logger.LogInformation($"Creating Cashfree payment for user {userId}, amount {request.Amount}");
            
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Cashfree credentials not found in configuration");
                return BadRequest(new { error = "Cashfree configuration missing" });
            }
            
            var baseUrl = "https://sandbox.cashfree.com/pg";
            var orderId = $"order_{Guid.NewGuid().ToString("N")[..10]}";
            
            var orderData = new
            {
                order_id = orderId,
                order_amount = (request.Amount / 100.0).ToString("F2"),
                order_currency = "INR",
                customer_details = new
                {
                    customer_id = userId ?? "unknown",
                    customer_phone = "9999999999",
                    customer_email = "user@example.com"
                },
                order_meta = new
                {
                    return_url = $"{Request.Scheme}://{Request.Host}/api/payment/cashfree/callback",
                    notify_url = $"{Request.Scheme}://{Request.Host}/api/payment/cashfree/webhook"
                }
            };
            
            var json = JsonSerializer.Serialize(orderData);
            _logger.LogInformation($"Cashfree request payload: {json}");
            
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-client-id", appId);
            client.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync($"{baseUrl}/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Cashfree response status: {response.StatusCode}, content: {responseContent}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Cashfree order creation failed with status {response.StatusCode}: {responseContent}");
                return BadRequest(new { 
                    error = "Failed to create Cashfree order",
                    details = responseContent,
                    statusCode = (int)response.StatusCode
                });
            }
            
            var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Safe property access
            var paymentSessionId = orderResponse.TryGetProperty("payment_session_id", out var sessionProp) 
                ? sessionProp.GetString() : null;
            
            var paymentUrl = "";
            if (orderResponse.TryGetProperty("payment_links", out var linksProp) && 
                linksProp.TryGetProperty("web", out var webProp))
            {
                paymentUrl = webProp.GetString() ?? "";
            }
            
            if (string.IsNullOrEmpty(paymentSessionId))
            {
                _logger.LogError($"Missing payment_session_id in Cashfree response: {responseContent}");
                return BadRequest(new { error = "Invalid Cashfree response - missing session ID" });
            }
            
            // Log both Payment and Transaction records
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = Guid.Parse(request.JobId),
                PayerId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                PayeeId = Guid.Parse(request.ProviderId),
                Amount = request.Amount / 100m,
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.UPI,
                TransactionId = orderId,
                PaymentIntentId = orderId,
                CreatedAt = DateTime.UtcNow
            };
            
            var transaction = new Transaction
            {
                PaymentIntentId = orderId,
                UserId = userId ?? "unknown",
                JobId = request.JobId,
                ProviderId = request.ProviderId,
                Amount = request.Amount,
                Currency = "inr",
                Status = "pending",
                Quote = request.Quote,
                PlatformFee = request.PlatformFee,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Payments.Add(payment);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Cashfree order created successfully: {orderId} for user {userId}");
            
            return Ok(new {
                sessionId = paymentSessionId,
                paymentUrl = paymentUrl,
                orderId = orderId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreateCashfreePayment: {Message}", ex.Message);
            return BadRequest(new { 
                error = ex.Message,
                type = ex.GetType().Name,
                stackTrace = ex.StackTrace
            });
        }
    }
    
    [HttpPost("cashfree/process-upi")]
    public async Task<IActionResult> ProcessUpiPayment([FromBody] ProcessUpiRequest request)
    {
        try
        {
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            var baseUrl = "https://sandbox.cashfree.com/pg";
            
            // Find transaction by session ID (stored in PaymentIntentId for now)
            var sessionParts = request.SessionId.Split('_');
            var searchTerm = sessionParts.Length > 1 ? sessionParts[1] : request.SessionId;
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId.Contains(searchTerm));
                
            if (transaction == null)
            {
                return NotFound(new { error = "Transaction not found" });
            }
            
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-client-id", appId);
            client.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");
            
            var response = await client.GetAsync($"{baseUrl}/orders/{transaction.PaymentIntentId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { 
                    status = "FAILED",
                    message = "Failed to get payment status",
                    transactionId = transaction.PaymentIntentId
                });
            }
            
            var orderData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var orderStatus = orderData.GetProperty("order_status").GetString();
            
            var status = orderStatus?.ToUpper() switch
            {
                "PAID" => "SUCCESS",
                "ACTIVE" => "PENDING",
                _ => "FAILED"
            };
            
            transaction.Status = orderStatus?.ToLower() ?? "pending";
            transaction.UpdatedAt = DateTime.UtcNow;
            
            // Also update Payment table
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == transaction.PaymentIntentId);
            if (payment != null)
            {
                payment.Status = orderStatus?.ToLower() switch
                {
                    "paid" => PaymentStatus.Completed,
                    "active" => PaymentStatus.Processing,
                    _ => PaymentStatus.Failed
                };
                if (payment.Status == PaymentStatus.Completed)
                {
                    payment.CompletedAt = DateTime.UtcNow;
                }
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new {
                status = status,
                message = status == "SUCCESS" ? "Payment completed successfully" : 
                         status == "PENDING" ? "Payment is being processed" : "Payment failed",
                transactionId = transaction.PaymentIntentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UPI payment");
            return BadRequest(new { 
                status = "FAILED",
                message = ex.Message,
                transactionId = ""
            });
        }
    }
    
    [HttpGet("cashfree/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> CashfreeCallback([FromQuery] string order_id, [FromQuery] string order_token)
    {
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == order_id);
                
            if (transaction != null)
            {
                // Verify payment status with Cashfree
                var appId = _configuration["CashFreeAPPID"];
                var secretKey = _configuration["cashfreesecretkey"];
                var baseUrl = "https://sandbox.cashfree.com/pg";
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-client-id", appId);
                client.DefaultRequestHeaders.Add("x-client-secret", secretKey);
                client.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");
                
                var response = await client.GetAsync($"{baseUrl}/orders/{order_id}");
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var orderData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var orderStatus = orderData.GetProperty("order_status").GetString();
                    
                    transaction.Status = orderStatus?.ToLower() ?? "pending";
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            
            var deepLink = $"zentroapp://payment/callback?order_id={order_id}&status={transaction?.Status ?? "unknown"}";
            return Redirect(deepLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Cashfree callback");
            return Redirect($"zentroapp://payment/callback?error={Uri.EscapeDataString(ex.Message)}");
        }
    }
    
    [HttpPost("cashfree/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> CashfreeWebhook([FromBody] JsonElement webhookData)
    {
        try
        {
            var orderId = webhookData.GetProperty("data").GetProperty("order").GetProperty("order_id").GetString();
            var orderStatus = webhookData.GetProperty("data").GetProperty("order").GetProperty("order_status").GetString();
            
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == orderId);
                
            if (transaction != null)
            {
                transaction.Status = orderStatus?.ToLower() ?? "pending";
                transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Cashfree webhook processed: {orderId}, Status: {orderStatus}");
            }
            
            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Cashfree webhook");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // Stripe Endpoints
    [HttpPost("stripe/create-payment-intent")]
    public async Task<IActionResult> CreateStripePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = "inr",
                PaymentMethodTypes = new List<string> { "upi" },
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

    [HttpPost("handle-action")]
    public async Task<IActionResult> HandlePaymentAction([FromBody] HandleActionRequest request)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(request.PaymentIntentId);
            
            _logger.LogInformation($"Handling payment action for: {paymentIntent.Id}, Status: {paymentIntent.Status}");
            
            // Update transaction status
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentIntentId == request.PaymentIntentId);
                
            if (transaction != null)
            {
                transaction.Status = paymentIntent.Status;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            
            return Ok(new {
                status = paymentIntent.Status,
                requiresAction = paymentIntent.Status == "requires_action",
                nextAction = paymentIntent.NextAction,
                clientSecret = paymentIntent.ClientSecret,
                succeeded = paymentIntent.Status == "succeeded"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment action");
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

public class ProcessUpiRequest
{
    public string SessionId { get; set; } = string.Empty;
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

public class HandleActionRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
}