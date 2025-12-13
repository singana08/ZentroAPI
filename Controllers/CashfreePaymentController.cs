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
                .Include(sr => sr.Category)
                .Include(sr => sr.SubCategory)
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

    [HttpPost("cashfree/webhook")]
    public IActionResult CashfreeWebhook([FromBody] CashfreeWebhookPayload payload)
    {
        try
        {
            if (!VerifySignature(payload)) 
                return Unauthorized();

            UpdateOrderStatus(payload.OrderId, payload.OrderStatus);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for order {OrderId}", payload?.OrderId);
            return BadRequest();
        }
    }

    private bool VerifySignature(CashfreeWebhookPayload payload)
    {
        // TODO: Implement signature verification
        return true;
    }

    private void UpdateOrderStatus(string orderId, string status)
    {
        // TODO: Update database with payment status
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
    }
}

public class OrderRequest
{
    public required string RequestId { get; set; }
    public decimal Amount { get; set; }
    public required string ProviderId { get; set; }
}

public class CashfreeWebhookPayload
{
    public required string OrderId { get; set; }
    public required string OrderStatus { get; set; }
    public decimal OrderAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
}