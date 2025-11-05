using HaluluAPI.Data;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OtpService> _logger;
    private readonly int _otpExpirationMinutes;
    private readonly int _otpLength;

    public OtpService(
        ApplicationDbContext context,
        ILogger<OtpService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _otpExpirationMinutes = configuration.GetValue<int>("OtpSettings:ExpirationMinutes", 5);
        _otpLength = configuration.GetValue<int>("OtpSettings:OtpLength", 6);
    }

    public async Task<(string OtpCode, DateTime ExpiresAt)> GenerateOtpAsync(
        string email,
        string? userId = null,
        OtpPurpose purpose = OtpPurpose.Authentication)
    {
        try
        {
            // Invalidate previous OTPs for this email and purpose
            var previousOtps = await _context.OtpRecords
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in previousOtps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            // Generate new OTP
            var otpCode = GenerateRandomOtp(_otpLength);
            var expiresAt = DateTime.UtcNow.AddMinutes(_otpExpirationMinutes);

            var otpRecord = new OtpRecord
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otpCode,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                Purpose = purpose,
                UserId = string.IsNullOrEmpty(userId) ? null : Guid.TryParse(userId, out var uid) ? uid : null
            };

            _context.OtpRecords.Add(otpRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"OTP generated for email: {email}");
            return (otpCode, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating OTP for email: {email}");
            throw;
        }
    }

    public async Task<bool> VerifyOtpAsync(
        string email,
        string otpCode,
        OtpPurpose purpose = OtpPurpose.Authentication)
    {
        try
        {
            // Check if email is locked
            if (await IsEmailLockedAsync(email))
            {
                _logger.LogWarning($"Email {email} is locked due to too many failed attempts");
                return false;
            }

            var otpRecord = await _context.OtpRecords
                .Where(o => o.Email == email &&
                           o.Purpose == purpose &&
                           !o.IsUsed && 
                           !o.IsLocked &&
                           o.ExpiresAt.ToUniversalTime() > DateTime.UtcNow
                           )
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                _logger.LogWarning($"No valid OTP found for email: {email}");
                return false;
            }

            if (otpRecord.OtpCode != otpCode)
            {
                otpRecord.AttemptCount++;

                if (otpRecord.AttemptCount >= otpRecord.MaxAttempts)
                {
                    otpRecord.IsLocked = true;
                    _logger.LogWarning($"OTP locked for email: {email} due to max attempts");
                }

                await _context.SaveChangesAsync();
                _logger.LogWarning($"Invalid OTP attempt for email: {email}");
                return false;
            }

            // OTP is valid
            otpRecord.IsUsed = true;
            otpRecord.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"OTP verified successfully for email: {email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying OTP for email: {email}");
            throw;
        }
    }

    public async Task InvalidateOtpAsync(string email)
    {
        try
        {
            var otpRecords = await _context.OtpRecords
                .Where(o => o.Email == email && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in otpRecords)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"OTPs invalidated for email: {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error invalidating OTP for email: {email}");
            throw;
        }
    }

    public async Task<bool> IsEmailLockedAsync(string email)
    {
        var lockedRecord = await _context.OtpRecords
            .Where(o => o.Email == email && o.IsLocked)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (lockedRecord == null)
            return false;

        // Check if lock is still valid (within last hour)
        var lockAge = DateTime.UtcNow - lockedRecord.CreatedAt;
        return lockAge.TotalHours < 1;
    }

    public async Task<TimeSpan?> GetOtpRemainingTimeAsync(string email)
    {
        var otpRecord = await _context.OtpRecords
            .Where(o => o.Email == email && !o.IsUsed && !o.IsLocked)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
            return null;

        var remaining = otpRecord.ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    private static string GenerateRandomOtp(int length)
    {
        var random = new Random();
        var otp = string.Empty;

        for (int i = 0; i < length; i++)
        {
            otp += random.Next(0, 10).ToString();
        }

        return otp;
    }
}