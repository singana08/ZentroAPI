namespace ZentroAPI.Services;

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public required object User { get; set; }
}