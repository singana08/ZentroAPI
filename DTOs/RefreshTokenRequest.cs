using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}

public class LogoutRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}