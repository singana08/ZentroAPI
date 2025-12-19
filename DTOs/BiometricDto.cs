namespace ZentroAPI.DTOs;

public class EnableBiometricResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string BiometricPin { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class BiometricLoginRequest
{
    public string BiometricPin { get; set; } = string.Empty;
}

public class BiometricLoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UserDto? User { get; set; }
}