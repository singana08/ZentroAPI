using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;

namespace ZentroAPI.Services;

public class BiometricService : IBiometricService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BiometricService> _logger;

    public BiometricService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<BiometricService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateBiometricPinAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var pin = GenerateSecurePin();
        var hashedPin = HashPin(pin);
        var expirationDays = _configuration.GetValue<int>("BiometricSettings:PinExpirationDays", 30);

        user.BiometricPin = hashedPin;
        user.BiometricPinExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
        user.BiometricEnabled = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Biometric PIN generated for user {UserId}", userId);
        return pin;
    }

    public async Task<bool> ValidateBiometricPinAsync(string pin)
    {
        var hashedPin = HashPin(pin);
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.BiometricPin == hashedPin && u.BiometricEnabled);

        if (user == null)
            return false;

        if (user.BiometricPinExpiresAt == null || user.BiometricPinExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired biometric PIN for user {UserId}", user.Id);
            return false;
        }

        return true;
    }

    public async Task<bool> DisableBiometricAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.BiometricPin = null;
        user.BiometricPinExpiresAt = null;
        user.BiometricEnabled = false;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Biometric disabled for user {UserId}", userId);
        return true;
    }

    private string GenerateSecurePin()
    {
        return Guid.NewGuid().ToString("N");
    }

    private string HashPin(string pin)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(pin);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
