namespace HaluluAPI.Services;

/// <summary>
/// Interface for OTP generation and validation
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate and store OTP for email
    /// </summary>
    Task<(string OtpCode, DateTime ExpiresAt)> GenerateOtpAsync(string email, string? userId = null, Models.OtpPurpose purpose = Models.OtpPurpose.Authentication);

    /// <summary>
    /// Verify OTP code
    /// </summary>
    Task<bool> VerifyOtpAsync(string email, string otpCode, Models.OtpPurpose purpose = Models.OtpPurpose.Authentication);

    /// <summary>
    /// Invalidate OTP after use
    /// </summary>
    Task InvalidateOtpAsync(string email);

    /// <summary>
    /// Check if email is locked due to too many failed attempts
    /// </summary>
    Task<bool> IsEmailLockedAsync(string email);

    /// <summary>
    /// Get remaining time for current OTP
    /// </summary>
    Task<TimeSpan?> GetOtpRemainingTimeAsync(string email);
}