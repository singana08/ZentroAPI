namespace HaluluAPI.Services;

/// <summary>
/// Interface for email sending functionality
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send OTP email
    /// </summary>
    Task<bool> SendOtpEmailAsync(string recipientEmail, string otpCode, int expirationMinutes);

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string recipientEmail, string firstName, string userRole);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string resetCode);
}