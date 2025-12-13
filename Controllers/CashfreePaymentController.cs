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
        
        _logger.LogInformation("Cashfree Payment controller initialized");
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        var appId = _configuration["CashFreeAPPID"];
        var secretKey = _configuration["cashfreesecretkey"];
        
        return Ok(new {
            hasAppId = !string.IsNullOrEmpty(appId),
            hasSecretKey = !string.IsNullOrEmpty(secretKey),
            appIdLength = appId?.Length ?? 0,
            message = "Cashfree controller is working"
        });
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
        try
        {
            _logger.LogInformation($"Creating Cashfree order for user {userId}, amount {request.Amount}");
            
            var orderId = $"order_{Guid.NewGuid().ToString("N")[..12]}";
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            var baseUrl = "https://sandbox.cashfree.com";
            
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Cashfree credentials not found");
                return BadRequest(new { error = "Cashfree configuration missing" });
            }
            
            var orderData = new
            {
                order_amount = request.Amount,
                order_currency = "INR",
                order_id = orderId,
                customer_details = new
                {
                    customer_id = userId ?? "guest",
                    customer_phone = request.CustomerPhone
                },
                order_meta = new
                {
                    return_url = $"https://www.cashfree.com/devstudio/preview/pg/mobile/hybrid?order_id={orderId}"
                }
            };
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", appId);
            _httpClient.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            _httpClient.DefaultRequestHeaders.Add("x-api-version", "2025-01-01");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            var json = JsonSerializer.Serialize(orderData);
            _logger.LogInformation($"Cashfree request: {json}");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{baseUrl}/pg/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Cashfree response status: {response.StatusCode}, content: {responseContent}");
            
            if (response.IsSuccessStatusCode)
            {
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    ServiceRequestId = Guid.Parse(request.JobId),
                    PayerId = Guid.Parse(userId ?? Guid.Empty.ToString()),
                    PayeeId = Guid.Parse(request.ProviderId),
                    Amount = request.Amount,
                    Status = PaymentStatus.Pending,
                    Method = PaymentMethod.UPI,
                    TransactionId = orderId,
                    PaymentIntentId = orderId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                
                var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Try to get optional properties safely
                string? paymentSessionId = null;
                string? orderToken = null;
                
                if (orderResponse.TryGetProperty("payment_session_id", out var sessionProp))
                {
                    paymentSessionId = sessionProp.GetString();
                }
                
                if (orderResponse.TryGetProperty("order_token", out var tokenProp))
                {
                    orderToken = tokenProp.GetString();
                }
                
                //return Ok(new {
                //    order_id = orderId,
                //    payment_session_id = paymentSessionId,
                //    order_token = orderToken,
                //    amount = request.Amount,
                //    currency = "INR"
                //});

                return Ok(new
                {
                    orderId = orderId,
                    paymentSessionId = paymentSessionId,
                    amount = request.Amount,
                    currency = "INR"
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

    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetPaymentStatus(string orderId)
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
            
            var response = await _httpClient.GetAsync($"{baseUrl}/pg/orders/{orderId}/payments");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var paymentsData = JsonSerializer.Deserialize<JsonElement>(content);
                
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == orderId);
                    
                if (payment != null && paymentsData.TryGetProperty("data", out var payments) && payments.GetArrayLength() > 0)
                {
                    var latestPayment = payments[0];
                    var status = latestPayment.GetProperty("payment_status").GetString();
                    
                    payment.Status = status switch
                    {
                        "SUCCESS" => PaymentStatus.Completed,
                        "PENDING" => PaymentStatus.Processing,
                        "FAILED" => PaymentStatus.Failed,
                        "CANCELLED" => PaymentStatus.Failed,
                        _ => PaymentStatus.Pending
                    };
                    
                    if (payment.Status == PaymentStatus.Completed)
                    {
                        payment.CompletedAt = DateTime.UtcNow;
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                return Ok(JsonSerializer.Deserialize<object>(content));
            }
            
            return BadRequest(new { error = content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {OrderId}", orderId);
            return BadRequest(new { error = ex.Message });
        }
    }
}


//public class CreateOrderRequest
//{
//    public decimal Amount { get; set; }
//    public required string JobId { get; set; }
//    public required string ProviderId { get; set; }
//    public string CustomerPhone { get; set; } = "9876543210";
//}

public class CreateOrderRequest
{
    public decimal Amount { get; set; }
    public required string JobId { get; set; }
    public required string ProviderId { get; set; }
    public decimal Quote { get; set; }
    public decimal PlatformFee { get; set; }
    public string CustomerPhone { get; set; } = "9876543210";
    public string ReturnUrl { get; set; } = "";
    public string NotifyUrl { get; set; } = "";
}
