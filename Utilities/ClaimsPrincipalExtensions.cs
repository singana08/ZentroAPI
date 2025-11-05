using System.Security.Claims;

namespace HaluluAPI.Utilities;

/// <summary>
/// Extension methods for ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get user ID from claims
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Get email from claims
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Get phone number from claims
    /// </summary>
    public static string? GetPhoneNumber(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.MobilePhone)?.Value;
    }

    /// <summary>
    /// Get user name from claims
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Get user role from claims
    /// </summary>
    public static string? GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Check if profile is complete
    /// </summary>
    public static bool IsProfileComplete(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("IsProfileComplete")?.Value;
        return bool.TryParse(value, out var result) && result;
    }

    /// <summary>
    /// Check if user is active
    /// </summary>
    public static bool IsActive(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("IsActive")?.Value;
        return bool.TryParse(value, out var result) && result;
    }
}