using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ZentroAPI.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderPassword;
    private readonly string _senderName;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _senderEmail = configuration["EmailSenderEmail"] ?? string.Empty;
        _senderPassword = configuration["EmailSenderPassword"] ?? string.Empty;
        _smtpHost = configuration["SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.TryParse(configuration["SmtpPort"], out var port) ? port : 587;
        _senderName = configuration["EmailSettings:SenderName"] ?? "Zentro";
        
        _logger.LogInformation($"Email config - Host: {_smtpHost}, Port: {_smtpPort}, Email: {(!string.IsNullOrEmpty(_senderEmail) ? "Found" : "Empty")}");
    }

    public async Task<bool> SendOtpEmailAsync(string recipientEmail, string otpCode, int expirationMinutes)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(string.Empty, recipientEmail));
            message.Subject = "Your Halulu Authentication Code";

            var htmlBody = GetOtpEmailTemplate(otpCode, expirationMinutes);
            message.Body = new TextPart("html") { Text = htmlBody };

            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending OTP email to {recipientEmail}");
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string recipientEmail, string firstName, string userRole)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(string.Empty, recipientEmail));
            message.Subject = "Welcome to Halulu!";

            var htmlBody = GetWelcomeEmailTemplate(firstName, userRole);
            message.Body = new TextPart("html") { Text = htmlBody };

            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending welcome email to {recipientEmail}");
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string resetCode)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(string.Empty, recipientEmail));
            message.Subject = "Reset Your Halulu Password";

            var htmlBody = GetPasswordResetEmailTemplate(resetCode);
            message.Body = new TextPart("html") { Text = htmlBody };

            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending password reset email to {recipientEmail}");
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(MimeMessage message)
    {
        try
        {
            using var client = new SmtpClient();
            
            // Disable SSL/TLS validation for development (not recommended for production)
            // Comment out or remove in production
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_senderEmail, _senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email sent to {message.To}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return false;
        }
    }

    private static string GetOtpEmailTemplate(string otpCode, int expirationMinutes)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .otp-code {{ font-size: 32px; font-weight: bold; color: #4CAF50; text-align: center; margin: 20px 0; letter-spacing: 5px; }}
                    .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Halulu Authentication</h1>
                    </div>
                    <div class='content'>
                        <p>Hello,</p>
                        <p>Your authentication code is:</p>
                        <div class='otp-code'>{otpCode}</div>
                        <p>This code will expire in <strong>{expirationMinutes} minutes</strong>.</p>
                        <p>If you didn't request this code, please ignore this email.</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Halulu. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private static string GetWelcomeEmailTemplate(string firstName, string userRole)
    {
        var roleText = userRole.ToLower() == "provider" ? "Service Provider" : "Service Requester";
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Welcome to Halulu!</h1>
                    </div>
                    <div class='content'>
                        <p>Hello {firstName},</p>
                        <p>Welcome to Halulu! Your account has been successfully created as a <strong>{roleText}</strong>.</p>
                        <p>You can now access all the features available on our platform.</p>
                        <p>If you have any questions or need assistance, feel free to reach out to our support team.</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Halulu. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    private static string GetPasswordResetEmailTemplate(string resetCode)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Password Reset Request</h1>
                    </div>
                    <div class='content'>
                        <p>Hello,</p>
                        <p>We received a request to reset your password. Use the code below to proceed:</p>
                        <p style='font-size: 24px; font-weight: bold; color: #4CAF50; text-align: center;'>{resetCode}</p>
                        <p>This code will expire in 10 minutes.</p>
                        <p>If you didn't request this, please ignore this email.</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Halulu. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }
}
