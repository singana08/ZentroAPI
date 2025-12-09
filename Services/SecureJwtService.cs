using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ZentroAPI.Services;

/// <summary>
/// Enhanced JWT service with stronger security
/// </summary>
public class SecureJwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecureJwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public SecureJwtService(IConfiguration configuration, ILogger<SecureJwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Read from Key Vault or configuration with fallback
        _secretKey = configuration["JwtSecretKey"] 
            ?? configuration["JwtSettings:SecretKey"] 
            ?? "TempSecretKeyForStartup123456789012345678901234567890";
            
        _logger.LogInformation($"JWT Secret Key source: {(configuration["JwtSecretKey"] != null ? "Key Vault" : "Fallback")}");
            
        _issuer = configuration["JwtSettings:Issuer"] ?? "ZentroAPI";
        _audience = configuration["JwtSettings:Audience"] ?? "ZentroMobileApp";
        _expirationMinutes = configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 1440);

        // Validate secret key strength
        ValidateSecretKey(_secretKey);
    }

    public string GenerateToken(Models.User user, string? activeRole = null, Guid? profileId = null)
    {
        try
        {
            var key = GetSigningKey();
            var tokenHandler = new JwtSecurityTokenHandler();

            // Determine role - use activeRole if provided, otherwise default to Requester
            var role = activeRole ?? "Requester";

            // Add additional security claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("user_id", user.Id.ToString()), // Add user_id claim for API compatibility
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, role),
                new("jti", Guid.NewGuid().ToString()), // JWT ID for tracking
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new("nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add profile ID if provided
            if (profileId.HasValue)
            {
                claims.Add(new("profile_id", profileId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                // Add additional security
                NotBefore = DateTime.UtcNow,
                IssuedAt = DateTime.UtcNow
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("JWT token generated for user: {UserId}", user.Id);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);
            throw;
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var key = GetSigningKey();
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // No tolerance for clock skew
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Additional validation for JWT
            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Invalid JWT algorithm or token type");
                return false;
            }

            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token has expired");
            return false;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating JWT token");
            return false;
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var key = GetSigningKey();
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        // Support both Base64 and raw string keys
        byte[] keyBytes;
        
        try
        {
            // Try Base64 first (recommended)
            keyBytes = Convert.FromBase64String(_secretKey);
        }
        catch
        {
            // Fallback to UTF8 encoding
            keyBytes = Encoding.UTF8.GetBytes(_secretKey);
        }

        return new SymmetricSecurityKey(keyBytes);
    }

    private void ValidateSecretKey(string secretKey)
    {
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT secret key cannot be null or empty");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(secretKey);
        }
        catch
        {
            keyBytes = Encoding.UTF8.GetBytes(secretKey);
        }

        // Require minimum 256 bits (32 bytes) for HS256
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException($"JWT secret key must be at least 32 bytes (256 bits). Current: {keyBytes.Length} bytes");
        }

        // Check for weak patterns
        if (secretKey.Contains("password") || secretKey.Contains("secret") || secretKey.Contains("key"))
        {
            _logger.LogWarning("JWT secret key contains predictable words - consider using cryptographically random key");
        }

        _logger.LogInformation("JWT secret key validation passed - {KeyLength} bytes", keyBytes.Length);
    }
}