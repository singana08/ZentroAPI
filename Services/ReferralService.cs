using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public class ReferralService : IReferralService
{
    private readonly ApplicationDbContext _context;
    private readonly IWalletService _walletService;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(ApplicationDbContext context, IWalletService walletService, ILogger<ReferralService> logger)
    {
        _context = context;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ReferralCodeDto? Data)> GetUserReferralCodeAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return (false, "User not found", null);

            // Generate referral code if not exists
            if (string.IsNullOrEmpty(user.ReferralCode))
            {
                user.ReferralCode = GenerateReferralCode();
                await _context.SaveChangesAsync();
            }

            var stats = await GetReferralStatsInternal(userId);

            var data = new ReferralCodeDto
            {
                ReferralCode = user.ReferralCode,
                TotalReferrals = stats.TotalReferrals,
                TotalEarnings = stats.TotalEarnings
            };

            return (true, "Referral code retrieved successfully", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referral code for user {UserId}", userId);
            return (false, "Failed to get referral code", null);
        }
    }

    public async Task<(bool Success, string Message)> UseReferralCodeAsync(Guid userId, string referralCode)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return (false, "User not found");

            if (user.ReferredById.HasValue)
                return (false, "You have already used a referral code");

            var referrer = await _context.Users.FirstOrDefaultAsync(u => u.ReferralCode == referralCode);
            if (referrer == null)
                return (false, "Invalid referral code");

            if (referrer.Id == userId)
                return (false, "You cannot use your own referral code");

            // Check if referral already exists
            var existingReferral = await _context.Set<Referral>()
                .FirstOrDefaultAsync(r => r.ReferrerId == referrer.Id && r.ReferredUserId == userId);
            if (existingReferral != null)
                return (false, "Referral already exists");

            // Create referral record
            var referral = new Referral
            {
                ReferrerId = referrer.Id,
                ReferredUserId = userId,
                ReferralCode = referralCode,
                Status = ReferralStatus.Pending
            };

            user.ReferredById = referrer.Id;

            _context.Set<Referral>().Add(referral);
            await _context.SaveChangesAsync();

            return (true, "Referral code applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using referral code {Code} for user {UserId}", referralCode, userId);
            return (false, "Failed to apply referral code");
        }
    }

    public async Task<(bool Success, string Message, ReferralStatsDto? Data)> GetReferralStatsAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return (false, "User not found", null);

            var stats = await GetReferralStatsInternal(userId);
            return (true, "Referral stats retrieved successfully", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referral stats for user {UserId}", userId);
            return (false, "Failed to get referral stats", null);
        }
    }

    public async Task ProcessReferralCompletionAsync(Guid userId)
    {
        try
        {
            // Find pending referrals where this user was referred
            var pendingReferrals = await _context.Set<Referral>()
                .Where(r => r.ReferredUserId == userId && r.Status == ReferralStatus.Pending)
                .ToListAsync();

            foreach (var referral in pendingReferrals)
            {
                // Add credit to referrer's wallet
                var creditResult = await _walletService.AddCreditAsync(
                    referral.ReferrerId,
                    referral.BonusAmount,
                    TransactionSource.ReferralBonus,
                    $"Referral bonus for {userId}",
                    referral.Id,
                    DateTime.UtcNow.AddDays(60) // 60-day expiry
                );

                if (creditResult.Success)
                {
                    referral.Status = ReferralStatus.Completed;
                    referral.CompletedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing referral completion for user {UserId}", userId);
        }
    }

    private async Task<ReferralStatsDto> GetReferralStatsInternal(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        var referrals = await _context.Set<Referral>()
            .Include(r => r.ReferredUser)
            .Where(r => r.ReferrerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var recentReferrals = referrals.Take(10).Select(r => new ReferralDto
        {
            Id = r.Id,
            ReferredUserName = r.ReferredUser.FullName,
            ReferredUserEmail = r.ReferredUser.Email,
            Status = r.Status.ToString(),
            BonusAmount = r.BonusAmount,
            CreatedAt = r.CreatedAt,
            CompletedAt = r.CompletedAt
        }).ToList();

        return new ReferralStatsDto
        {
            ReferralCode = user?.ReferralCode ?? "",
            TotalReferrals = referrals.Count,
            PendingReferrals = referrals.Count(r => r.Status == ReferralStatus.Pending),
            CompletedReferrals = referrals.Count(r => r.Status == ReferralStatus.Completed),
            TotalEarnings = referrals.Where(r => r.Status == ReferralStatus.Completed).Sum(r => r.BonusAmount),
            PendingEarnings = referrals.Where(r => r.Status == ReferralStatus.Pending).Sum(r => r.BonusAmount),
            RecentReferrals = recentReferrals
        };
    }

    private string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}