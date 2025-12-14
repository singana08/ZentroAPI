using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Interface for authentication business logic
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Send OTP to email or phone and create OTP record
    /// </summary>
    Task<(bool Success, string Message, int ExpirationMinutes)> SendOtpAsync(string? email, string? phoneNumber);

    /// <summary>
    /// Verify OTP and return JWT token
    /// </summary>
    Task<(bool Success, string Message, string? Token, UserDto? User, bool IsNewUser)> VerifyOtpAsync(string? email, string? phoneNumber, string otpCode);

    /// <summary>
    /// Register/complete profile for new user and return JWT token
    /// </summary>
    Task<(bool Success, string Message, string? Token, UserDto? User)> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Get current user information
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync(string userId);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Get user by phone number
    /// </summary>
    Task<UserDto?> GetUserByPhoneAsync(string phoneNumber);

    Task<(bool Success, string Message, string? Token, UserDto? User)> AddProfileAsync(string userId, AddProfileRequest request);

    /// <summary>
    /// Switch user role and return new token with appropriate profile ID
    /// </summary>
    Task<(bool Success, string Message, string? Token, UserDto? User)> SwitchRoleAsync(string userId, string newRole);

    /// <summary>
    /// Get user profile data by profile ID
    /// </summary>
    Task<ProfileDto?> GetUserProfileAsync(string profileId);
    
    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<(bool Success, string Message, TokenResponse? TokenResponse)> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    Task<(bool Success, string Message)> LogoutAsync(string refreshToken);
}
