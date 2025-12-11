using System.Security.Cryptography;
using System.Text;

namespace ZentroAPI.Services
{
    public interface IWebhookVerificationService
    {
        bool VerifyCashfreeSignature(string payload, string signature, string timestamp);
    }

    public class WebhookVerificationService : IWebhookVerificationService
    {
        private readonly string _webhookSecret;

        public WebhookVerificationService(IConfiguration configuration)
        {
            _webhookSecret = configuration["CashfreeWebhookSecret"] ?? "";
        }

        public bool VerifyCashfreeSignature(string payload, string signature, string timestamp)
        {
            if (string.IsNullOrEmpty(_webhookSecret) || string.IsNullOrEmpty(signature))
                return false;

            var signedPayload = timestamp + payload;
            var computedSignature = ComputeHmacSha256(_webhookSecret, signedPayload);
            
            return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string secret, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}