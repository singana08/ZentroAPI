using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CashfreePaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CashfreePaymentController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;

    public CashfreePaymentController(IConfiguration configuration, ILogger<CashfreePaymentController> logger, ApplicationDbContext context, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _httpClient = httpClient;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest req)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            
            // Get user details
            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null) return BadRequest(new { error = "User not found" });
            
            // Get service request details
            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.Id == Guid.Parse(req.RequestId));
            if (serviceRequest == null) return BadRequest(new { error = "Service request not found" });
            
            // Get provider details
            var provider = await _context.Users.FindAsync(Guid.Parse(req.ProviderId));
            if (provider == null) return BadRequest(new { error = "Provider not found" });
            
            var orderId = $"order_{Guid.NewGuid().ToString("N")[..12]}";
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            var baseUrl = "https://sandbox.cashfree.com";
            
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(secretKey))
                return BadRequest(new { error = "Cashfree configuration missing" });
            
            var orderData = new
            {
                order_amount = req.Amount,
                order_currency = "INR",
                order_id = orderId,
                customer_details = new
                {
                    customer_id = userId,
                    customer_email = user.Email,
                    customer_phone = user.PhoneNumber ?? "9999999999"
                }
            };
            
            // Create payment record for audit
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = serviceRequest.Id,
                PayerId = user.Id,
                PayeeId = provider.Id,
                Amount = req.Amount,
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.UPI,
                TransactionId = orderId,
                PaymentIntentId = orderId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", appId);
            _httpClient.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            _httpClient.DefaultRequestHeaders.Add("x-api-version", "2025-01-01");
            
            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{baseUrl}/pg/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var paymentLink = orderResponse.TryGetProperty("payment_link", out var linkProp) 
                    ? linkProp.GetString() 
                    : $"{baseUrl}/pg/orders/{orderId}/pay";
                
                return Ok(new {
                    orderId = orderId,
                    paymentLink = paymentLink
                });
            }

            return BadRequest(new { error = responseContent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Cashfree order");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrderStatus(string orderId)
    {
        try
        {
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            var baseUrl = "https://sandbox.cashfree.com";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", appId);
            _httpClient.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            _httpClient.DefaultRequestHeaders.Add("x-api-version", "2025-01-01");
            
            var response = await _httpClient.GetAsync($"{baseUrl}/pg/orders/{orderId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var orderData = JsonSerializer.Deserialize<JsonElement>(content);
                
                return Ok(new {
                    orderId = orderData.GetProperty("order_id").GetString(),
                    status = orderData.GetProperty("order_status").GetString(),
                    amount = orderData.GetProperty("order_amount").GetDecimal(),
                    currency = orderData.GetProperty("order_currency").GetString()
                });
            }
            
            return BadRequest(new { error = content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order status for {OrderId}", orderId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("verify/{orderId}")]
    public async Task<IActionResult> VerifyPaymentStatus(string orderId)
    {
        try
        {
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            var baseUrl = "https://sandbox.cashfree.com";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", appId);
            _httpClient.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            _httpClient.DefaultRequestHeaders.Add("x-api-version", "2025-01-01");
            
            var response = await _httpClient.GetAsync($"{baseUrl}/pg/orders/{orderId}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var orderData = JsonSerializer.Deserialize<JsonElement>(content);
                var cashfreeStatus = orderData.GetProperty("order_status").GetString();
                
                // Update local database with Cashfree status
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == orderId);
                    
                if (payment != null)
                {
                    var newStatus = cashfreeStatus switch
                    {
                        "PAID" => PaymentStatus.Completed,
                        "ACTIVE" => PaymentStatus.Processing,
                        "EXPIRED" => PaymentStatus.Failed,
                        "CANCELLED" => PaymentStatus.Failed,
                        _ => PaymentStatus.Pending
                    };
                    
                    if (payment.Status != newStatus)
                    {
                        payment.Status = newStatus;
                        if (newStatus == PaymentStatus.Completed)
                            payment.CompletedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                
                return Ok(new {
                    orderId = orderData.GetProperty("order_id").GetString(),
                    status = orderData.GetProperty("order_status").GetString(),
                    amount = orderData.GetProperty("order_amount").GetDecimal(),
                    currency = orderData.GetProperty("order_currency").GetString()
                });
            }
            
            return BadRequest(new { error = content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment status for {OrderId}", orderId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("record-payment")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest req)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            
            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.Id == Guid.Parse(req.RequestId));
            if (serviceRequest == null) return BadRequest(new { error = "Service request not found" });
            
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = serviceRequest.Id,
                PayerId = Guid.Parse(userId),
                PayeeId = Guid.Parse(req.ProviderId),
                Amount = req.Amount,
                Status = req.PaymentStatus == "SUCCESS" ? PaymentStatus.Completed : PaymentStatus.Failed,
                Method = PaymentMethod.UPI,
                TransactionId = req.TransactionId,
                PaymentIntentId = req.TransactionId,
                CreatedAt = req.PaymentDate,
                CompletedAt = req.PaymentStatus == "SUCCESS" ? req.PaymentDate : null
            };
            
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Payment recorded successfully", paymentId = payment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cashfree/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> CashfreeWebhook([FromBody] JsonElement payload)
    {
        try
        {
            var eventType = payload.GetProperty("type").GetString();
            var orderId = payload.GetProperty("order_id").GetString();
            
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == orderId);
                
            if (payment != null)
            {
                payment.Status = eventType switch
                {
                    "PAYMENT_SUCCESS_WEBHOOK" => PaymentStatus.Completed,
                    "PAYMENT_FAILED_WEBHOOK" => PaymentStatus.Failed,
                    "PAYMENT_USER_DROPPED_WEBHOOK" => PaymentStatus.Failed,
                    _ => payment.Status
                };
                
                if (payment.Status == PaymentStatus.Completed)
                {
                    payment.CompletedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment {OrderId} event {EventType} processed, status: {Status}", orderId, eventType, payment.Status);
            }
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing failed");
            return Ok();
        }
    }
}

public class OrderRequest
{
    public required string RequestId { get; set; }
    public decimal Amount { get; set; }
    public required string ProviderId { get; set; }
}

public class RecordPaymentRequest
{
    public required string RequestId { get; set; }
    public required string ProviderId { get; set; }
    public decimal Amount { get; set; }
    public required string PaymentMethod { get; set; }
    public required string TransactionId { get; set; }
    public required string PaymentStatus { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal ProviderAmount { get; set; }
}

