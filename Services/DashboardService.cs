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
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ProviderDashboardResponseDto? Data)> GetProviderDashboardAsync(
        Guid providerId)
    {
        try
        {
            // Get provider with user details
            var provider = await _context.Providers
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
            {
                return (false, "Provider not found", null);
            }

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

            var response = new ProviderDashboardResponseDto
            {
                TodayEarnings = todayEarnings,
                ActiveRequests = activeRequests,
                Rating = rating,
                CompletionRate = completionRate,
                NotificationCount = notificationCount,
                UserName = provider.User?.FullName ?? "Provider"
            };

            return (true, "Dashboard data retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data for provider {ProviderId}", providerId);
            return (false, "An error occurred while retrieving dashboard data", null);
        }
    }
}
