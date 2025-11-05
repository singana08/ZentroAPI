using HaluluAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HaluluAPI.Services;

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        IConfiguration configuration,
        ILogger<JwtService> logger)
    {
        _logger = logger;
        var jwtSettings = configuration.GetSection("JwtSettings");
        _secretKey = jwtSettings.GetValue<string>("SecretKey") ?? string.Empty;
        _issuer = jwtSettings.GetValue<string>("Issuer") ?? "HaluluAPI";
        _audience = jwtSettings.GetValue<string>("Audience") ?? "HaluluMobileApp";
        _expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 1440);

        if (_secretKey.Length < 32)
        {
            _logger.LogWarning("JWT Secret key is less than 32 characters. This is not recommended for production.");
        }
    }

    public string GenerateToken(User user, string? activeRole = null, Guid? profileId = null)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Determine active role: requester or provider
            string role = activeRole ?? "REQUESTER"; // Default to REQUESTER
            
            // Get profile ID based on role if not provided
            if (profileId == null || profileId == Guid.Empty)
            {
                if (role == "PROVIDER" && user.ProviderProfile != null)
                    profileId = user.ProviderProfile.Id;
                else if (user.RequesterProfile != null)
                    profileId = user.RequesterProfile.Id;
                else
                    profileId = user.Id; // Fallback to user ID
            }
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, profileId?.ToString() ?? user.Id.ToString()),
                new Claim("profile_id", profileId?.ToString() ?? user.Id.ToString()),
                new Claim("user_id", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("active_role", role),
                new Claim("has_requester_profile", (user.RequesterProfile != null).ToString().ToLower()),
                new Claim("has_provider_profile", (user.ProviderProfile != null).ToString().ToLower())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation($"JWT token generated for user: {user.Email} with role: {role}");
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating JWT token for user: {user.Email}");
            throw;
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return validatedToken is JwtSecurityToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            return jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting user ID from token");
            return null;
        }
    }
}