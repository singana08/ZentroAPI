using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for requesting OTP to be sent (either email or phone)
/// </summary>
public class SendOtpRequest
{
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public OtpPurposeDto Purpose { get; set; } = OtpPurposeDto.Authentication;
}

/// <summary>
/// DTO for verifying OTP (email or phone)
/// </summary>
public class VerifyOtpRequest
{
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "OTP is required")]
    [StringLength(10, MinimumLength = 4, ErrorMessage = "OTP must be between 4-10 characters")]
    public string OtpCode { get; set; } = string.Empty;

    public OtpPurposeDto Purpose { get; set; } = OtpPurposeDto.Authentication;
}

/// <summary>
/// DTO for user registration/profile completion
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2-200 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required (0 for REQUESTER, 1 for PROVIDER)")]
    public UserRoleDto Role { get; set; } = UserRoleDto.Requester;

    public UserRoleDto? DefaultRole { get; set; }

    // Optional: Email and PhoneNumber (already verified via OTP)
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
    public string? Address { get; set; }

    // Provider-specific fields (required if Role is Provider)
    public string[]? ServiceCategories { get; set; }
}

public enum UserRoleDto
{
    Requester = 0,
    Provider = 1
}

public enum OtpPurposeDto
{
    Authentication = 0,
    PhoneVerification = 1,
    PasswordReset = 2,
    EmailVerification = 3,
    PhoneChange = 4,
    EmailChange = 5
}