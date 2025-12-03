using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeWebhookController : ControllerBase
{
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly string _webhookSecret;

    public StripeWebhookController(IConfiguration configuration, ILogger<StripeWebhookController> logger)
    {
        _logger = logger;
        _webhookSecret = configuration["StripeSettings:WebhookSecret"] ?? "";
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];

            var stripeEvent = EventUtility.ConstructEvent(
                json, stripeSignature, _webhookSecret);

            _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogInformation("Payment succeeded: {PaymentIntentId}", paymentIntent?.Id);
                    // Handle successful payment
                    break;

                case Events.PaymentIntentPaymentFailed:
                    var failedPayment = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogWarning("Payment failed: {PaymentIntentId}", failedPayment?.Id);
                    // Handle failed payment
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error");
            return StatusCode(500);
        }
    }
}