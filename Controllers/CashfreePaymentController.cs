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

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("user_id")?.Value;
        
        try
        {
            var orderId = $"order_{Guid.NewGuid().ToString("N")[..12]}";
            var appId = _configuration["CashFreeAPPID"];
            var secretKey = _configuration["cashfreesecretkey"];
            var environment = _configuration["CashfreeEnvironment"] ?? "sandbox";
            var baseUrl = environment == "production" ? "https://api.cashfree.com" : "https://sandbox.cashfree.com";
            
            var orderData = new
            {
                order_id = orderId,
                order_amount = request.Amount,
                order_currency = "INR",
                customer_details = new
                {
                    customer_id = userId ?? "guest",
                    customer_email = request.CustomerEmail,
                    customer_phone = request.CustomerPhone
                },
                order_meta = new
                {
                    return_url = request.ReturnUrl,
                    notify_url = request.NotifyUrl
                },
                order_note = $"Payment for job {request.JobId}"
            };
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", appId);
            _httpClient.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            _httpClient.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");
            
            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{baseUrl}/pg/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
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
                
                return Ok(new {
                    order_id = orderId,
                    payment_session_id = orderResponse.GetProperty("payment_session_id").GetString(),
                    order_token = orderResponse.GetProperty("order_token").GetString(),
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
            var environment = _configuration["CashfreeEnvironment"] ?? "sandbox";
            var baseUrl = environment == "production" ? "https://api.cashfree.com" : "https://sandbox.cashfree.com";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", appId);
            _httpClient.DefaultRequestHeaders.Add("x-client-secret", secretKey);
            _httpClient.DefaultRequestHeaders.Add("x-api-version", "2023-08-01");
            
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

public class CreateOrderRequest
{
    public decimal Amount { get; set; }
    public string JobId { get; set; }
    public string ProviderId { get; set; }
    public string CustomerEmail { get; set; }
    public string CustomerPhone { get; set; }
    public string ReturnUrl { get; set; }
    public string NotifyUrl { get; set; }
}