using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentroAPI.Services;
using ZentroAPI.DTOs;
using ZentroAPI.Utilities;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("create-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        var result = await _paymentService.CreatePaymentIntentAsync(request.Amount, request.Currency ?? "usd");
        
        if (result.Success)
        {
            return Ok(new PaymentIntentResponse
            {
                Success = true,
                Message = result.Message,
                PaymentIntentId = result.PaymentIntentId
            });
        }

        return BadRequest(new ErrorResponse { Message = result.Message });
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentRequest request)
    {
        var userIdString = User.GetUserId();
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest(new ErrorResponse { Message = "Invalid user ID" });
        }
        
        var result = await _paymentService.ProcessPaymentAsync(
            request.ServiceRequestId, userId, request.PayeeId, request.Amount, request.PaymentIntentId);

        if (result.Success)
        {
            return Ok(new { success = true, message = result.Message });
        }

        return BadRequest(new ErrorResponse { Message = result.Message });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var userIdString = User.GetUserId();
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest(new ErrorResponse { Message = "Invalid user ID" });
        }
        
        var payments = await _paymentService.GetUserPaymentsAsync(userId);
        return Ok(payments);
    }
}