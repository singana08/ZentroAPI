using HaluluAPI.Models;

namespace HaluluAPI.Services;

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
}