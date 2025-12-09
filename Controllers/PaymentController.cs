using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}

public class CreatePaymentIntentRequest
{
    public long Amount { get; set; }
    public string JobId { get; set; }
    public string ProviderId { get; set; }
    public decimal Quote { get; set; }
    public decimal PlatformFee { get; set; }
}