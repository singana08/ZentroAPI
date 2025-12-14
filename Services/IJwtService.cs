using ZentroAPI.Models;

namespace ZentroAPI.Services;

/// <summary>
/// Interface for JWT token generation and validation
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token for authenticated user with optional active role and profile ID
    /// </summary>
    string GenerateToken(User user, string? activeRole = null, Guid? profileId = null);

    /// <summary>
    /// Validate JWT token
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// Get user ID from token
    /// </summary>
    string? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Generate token response with both access and refresh tokens
    /// </summary>
    Task<TokenResponse> GenerateTokenResponse(User user, string? activeRole = null, Guid? profileId = null, string? deviceId = null);
}
