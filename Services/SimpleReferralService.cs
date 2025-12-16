using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public class SimpleReferralService : IReferralService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SimpleReferralService> _logger;

    public SimpleReferralService(ApplicationDbContext context, ILogger<SimpleReferralService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ReferralCodeDto? Data)> GetUserReferralCodeAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (false, "User not found", null);

        if (string.IsNullOrEmpty(user.ReferralCode))
        {
            user.ReferralCode = GenerateReferralCode();
            await _context.SaveChangesAsync();
        }

        var totalReferrals = await _context.Users.CountAsync(u => u.ReferredById == userId);

        return (true, "Success", new ReferralCodeDto
        {
            ReferralCode = user.ReferralCode,
            TotalReferrals = totalReferrals,
            TotalEarnings = 0
        });
    }

    public async Task<(bool Success, string Message, ReferralStatsDto? Data)> GetReferralStatsAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (false, "User not found", null);

        if (string.IsNullOrEmpty(user.ReferralCode))
        {
            user.ReferralCode = GenerateReferralCode();
            await _context.SaveChangesAsync();
        }

        var referredUsers = await _context.Users
            .Where(u => u.ReferredById == userId)
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentReferrals = referredUsers.Select(u => new ReferralDto
        {
            Id = u.Id,
            ReferredUserName = u.FullName,
            ReferredUserEmail = u.Email,
            Status = "Completed",
            BonusAmount = 0,
            CreatedAt = u.CreatedAt,
            CompletedAt = u.CreatedAt
        }).ToList();

        var totalReferrals = await _context.Users.CountAsync(u => u.ReferredById == userId);

        return (true, "Success", new ReferralStatsDto
        {
            ReferralCode = user.ReferralCode,
            TotalReferrals = totalReferrals,
            PendingReferrals = 0,
            CompletedReferrals = totalReferrals,
            TotalEarnings = 0,
            PendingEarnings = 0,
            RecentReferrals = recentReferrals,
            Terms = new ReferralTermsDto()
        });
    }

    public async Task<(bool Success, string Message)> UseReferralCodeAsync(Guid userId, string referralCode)
    {
        return (true, "Not implemented in simple version");
    }

    public async Task ProcessReferralCompletionAsync(Guid userId)
    {
        // Not needed in simple version
    }

    public async Task ProcessFirstBookingBonusAsync(Guid bookingUserId, Guid bookingId, decimal bookingAmount)
    {
        // Not needed in simple version
    }

    public async Task<decimal> GetReferralDiscountAsync(Guid userId)
    {
        return 0m;
    }

    public async Task UseReferralDiscountAsync(Guid userId)
    {
        // Not needed in simple version
    }

    private string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}