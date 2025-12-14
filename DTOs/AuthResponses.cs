namespace ZentroAPI.DTOs;

/// <summary>
/// Response after OTP is sent
/// </summary>
public class SendOtpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
}

/// <summary>
/// Response after OTP verification
/// </summary>
public class VerifyOtpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserDto? User { get; set; }
    public bool IsNewUser { get; set; }
    public bool NeedsTokenRegistration { get; set; } = true;
}

/// <summary>
/// JWT Token response
/// </summary>
public class AuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = new();
    public bool NeedsTokenRegistration { get; set; } = true;
}

/// <summary>
/// Response after user registration/profile completion
/// </summary>
public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

/// <summary>
/// User DTO for responses - simplified to core identity only
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    
    // Role profile indicators
    public bool HasRequesterProfile { get; set; }
    public bool HasProviderProfile { get; set; }
    public bool IsProfileCompleted { get; set; }
    public string? DefaultRole { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Error response
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}
