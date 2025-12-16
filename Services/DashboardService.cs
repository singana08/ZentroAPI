using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Services;

/// <summary>
/// Service for provider dashboard summary data
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IReferralService _referralService;
    private readonly IWalletService _walletService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, IReferralService referralService, IWalletService walletService, ILogger<DashboardService> logger)
    {
        _context = context;
        _referralService = referralService;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ProviderDashboardResponseDto? Data)> GetProviderDashboardAsync(
        Guid providerId)
    {
        try
        {
            // Get provider with user details using proper join
            var providerWithUser = await _context.Providers
                .Join(_context.Users,
                    p => p.UserId,
                    u => u.Id,
                    (p, u) => new { Provider = p, User = u })
                .AsNoTracking()
                .FirstOrDefaultAsync(pu => pu.Provider.Id == providerId);

            if (providerWithUser == null)
            {
                return (false, "Provider not found", null);
            }

            var provider = providerWithUser.Provider;
            var user = providerWithUser.User;



            var today = DateTime.UtcNow.Date;
            var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

            // Calculate today's earnings from completed requests
            var todayEarnings = await _context.ServiceRequests
                .Where(sr => sr.AssignedProviderId == providerId 
                    && sr.Status == ServiceRequestStatus.Completed 
                    && sr.UpdatedAt.HasValue 
                    && sr.UpdatedAt.Value.Date == todayUtc)
                .Join(_context.Quotes.Where(q => q.ProviderId == providerId),
                    sr => sr.Id,
                    q => q.RequestId,
                    (sr, q) => q.Price)
                .SumAsync();

            // Count active requests (assigned status)
            var activeRequests = await _context.ServiceRequests
                .CountAsync(sr => sr.AssignedProviderId == providerId 
                    && sr.Status == ServiceRequestStatus.Assigned);

            // Get provider rating
            var rating = provider.Rating;

            // Calculate completion rate (this month)
            var startOfMonth = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1), DateTimeKind.Utc);
            var thisMonthRequests = await _context.ServiceRequests
                .Where(sr => sr.AssignedProviderId == providerId 
                    && sr.CreatedAt >= startOfMonth)
                .ToListAsync();

            var completionRate = 0;
            if (thisMonthRequests.Count > 0)
            {
                var completedCount = thisMonthRequests.Count(r => r.Status == ServiceRequestStatus.Completed);
                completionRate = (int)Math.Round((double)completedCount / thisMonthRequests.Count * 100);
            }

            // Count unread notifications
            var notificationCount = await _context.Notifications
                .CountAsync(n => n.ProfileId == providerId && !n.IsRead);

            // Get referral summary
            ReferralSummaryDto? referralSummary = null;
            try
            {
                _logger.LogInformation("Getting referral data for provider {ProviderId}, UserId: {UserId}", providerId, provider.UserId);
                
                var referralResult = await _referralService.GetReferralStatsAsync(provider.UserId);
                if (referralResult.Success && referralResult.Data != null)
                {
                    referralSummary = new ReferralSummaryDto
                    {
                        ReferralCode = referralResult.Data.ReferralCode,
                        TotalReferrals = referralResult.Data.TotalReferrals,
                        TotalEarnings = referralResult.Data.TotalEarnings,
                        PendingReferrals = referralResult.Data.PendingReferrals
                    };
                    _logger.LogInformation("Referral stats retrieved successfully for provider {ProviderId}", providerId);
                }
                else
                {
                    _logger.LogWarning("Referral stats failed for provider {ProviderId}: {Message}", providerId, referralResult.Message);
                    
                    // Create basic referral summary even if service fails
                    var codeResult = await _referralService.GetUserReferralCodeAsync(provider.UserId);
                    if (codeResult.Success && codeResult.Data != null)
                    {
                        referralSummary = new ReferralSummaryDto
                        {
                            ReferralCode = codeResult.Data.ReferralCode,
                            TotalReferrals = codeResult.Data.TotalReferrals,
                            TotalEarnings = codeResult.Data.TotalEarnings,
                            PendingReferrals = 0
                        };
                        _logger.LogInformation("Fallback referral code retrieved for provider {ProviderId}", providerId);
                    }
                    else
                    {
                        _logger.LogWarning("Fallback referral code also failed for provider {ProviderId}: {Message}", providerId, codeResult.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting referral data for provider {ProviderId}", providerId);
                // Don't fail dashboard if referral service fails
            }
            
            _logger.LogInformation("Dashboard referral summary for provider {providerId}: {Status}", providerId, referralSummary != null ? "Found" : "Null");
            

            var response = new ProviderDashboardResponseDto
            {
                TodayEarnings = todayEarnings,
                ActiveRequests = activeRequests,
                Rating = rating,
                CompletionRate = completionRate,
                NotificationCount = notificationCount,
                UserName = user.FullName,
                ReferralSummary = referralSummary
            };

            return (true, "Dashboard data retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data for provider {ProviderId}", providerId);
            return (false, "An error occurred while retrieving dashboard data", null);
        }
    }

    public async Task<(bool Success, string Message, RequesterDashboardResponseDto? Data)> GetRequesterDashboardAsync(
        Guid requesterId)
    {
        try
        {
            // Get requester with user details using proper join
            var requesterWithUser = await _context.Requesters
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new { Requester = r, User = u })
                .AsNoTracking()
                .FirstOrDefaultAsync(ru => ru.Requester.Id == requesterId);

            if (requesterWithUser == null)
            {
                return (false, "Requester not found", null);
            }

            var requester = requesterWithUser.Requester;
            var user = requesterWithUser.User;



            // Count active requests (anything except completed or cancelled)
            var activeRequests = await _context.ServiceRequests
                .CountAsync(sr => sr.RequesterId == requesterId 
                    && sr.Status != ServiceRequestStatus.Completed 
                    && sr.Status != ServiceRequestStatus.Cancelled);

            // Count completed requests
            var completedRequests = await _context.ServiceRequests
                .CountAsync(sr => sr.RequesterId == requesterId 
                    && sr.Status == ServiceRequestStatus.Completed);

            // Calculate total spent on completed requests
            var totalSpent = await _context.ServiceRequests
                .Where(sr => sr.RequesterId == requesterId 
                    && sr.Status == ServiceRequestStatus.Completed)
                .Join(_context.Quotes,
                    sr => sr.Id,
                    q => q.RequestId,
                    (sr, q) => q.Price)
                .SumAsync();

            // Count unread notifications
            var notificationCount = await _context.Notifications
                .CountAsync(n => n.ProfileId == requesterId && !n.IsRead);

            // Calculate saved amount (placeholder logic - 10% of total spent)
            var savedAmount = totalSpent * 0.1m;

            // Get scheduled services (requests with dates and assigned providers)
            var scheduledServices = await _context.ServiceRequests
                .Where(sr => sr.RequesterId == requesterId 
                    && sr.Date.HasValue 
                    && sr.Status != ServiceRequestStatus.Completed 
                    && sr.Status != ServiceRequestStatus.Cancelled)
                .OrderBy(sr => sr.Date)
                .Select(sr => new ScheduledServiceDto
                {
                    RequestId = sr.Id,
                    ProviderId = sr.AssignedProviderId,
                    MainCategory = sr.MainCategory,
                    SubCategory = sr.SubCategory,
                    Date = sr.Date,
                    Time = sr.Time,
                    Status = sr.Status.ToString()
                })
                .ToListAsync();

            // Get referral summary
            ReferralSummaryDto? referralSummary = null;
            WalletSummaryDto? walletSummary = null;
            
            try
            {
                var referralResult = await _referralService.GetReferralStatsAsync(requester.UserId);
                if (referralResult.Success && referralResult.Data != null)
                {
                    referralSummary = new ReferralSummaryDto
                    {
                        ReferralCode = referralResult.Data.ReferralCode,
                        TotalReferrals = referralResult.Data.TotalReferrals,
                        TotalEarnings = referralResult.Data.TotalEarnings,
                        PendingReferrals = referralResult.Data.PendingReferrals
                    };
                }
                else
                {
                    var codeResult = await _referralService.GetUserReferralCodeAsync(requester.UserId);
                    if (codeResult.Success && codeResult.Data != null)
                    {
                        referralSummary = new ReferralSummaryDto
                        {
                            ReferralCode = codeResult.Data.ReferralCode,
                            TotalReferrals = codeResult.Data.TotalReferrals,
                            TotalEarnings = codeResult.Data.TotalEarnings,
                            PendingReferrals = 0
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting referral data for requester {RequesterId}", requesterId);
            }

            try
            {
                var walletResult = await _walletService.GetWalletAsync(requester.UserId);
                if (walletResult.Success && walletResult.Data != null)
                {
                    var expiringCredits = walletResult.Data.RecentTransactions
                        .Where(t => t.Type == "Credit" && t.ExpiresAt.HasValue && t.ExpiresAt.Value <= DateTime.UtcNow.AddDays(7))
                        .ToList();
                    
                    walletSummary = new WalletSummaryDto
                    {
                        Balance = walletResult.Data.Balance,
                        ExpiringCredits = expiringCredits.Count,
                        ExpiringAmount = expiringCredits.Sum(t => t.Amount)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet data for requester {RequesterId}", requesterId);
            }
            
            _logger.LogInformation($"Dashboard data for requester {requesterId}: Referral={referralSummary != null}, Wallet={walletSummary != null}");

            var response = new RequesterDashboardResponseDto
            {
                UserName = user.FullName,
                ActiveRequests = activeRequests,
                CompletedServices = completedRequests,
                TotalSpent = totalSpent,
                SavedAmount = savedAmount,
                ScheduledServices = scheduledServices,
                ReferralSummary = referralSummary,
                WalletSummary = walletSummary
            };

            return (true, "Dashboard data retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data for requester {RequesterId}", requesterId);
            return (false, "An error occurred while retrieving dashboard data", null);
        }
    }
}
