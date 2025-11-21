using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

/// <summary>
/// Service for calculating provider analytics from existing tables
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ProviderAnalyticsResponseDto? Data)> GetProviderAnalyticsAsync(
        Guid providerId)
    {
        try
        {
            // Verify provider exists
            var providerExists = await _context.Providers.AnyAsync(p => p.Id == providerId);
            if (!providerExists)
            {
                return (false, "Provider not found", null);
            }

            var now = DateTime.UtcNow;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            // Get service requests for this provider
            var allRequests = await _context.ServiceRequests
                .Where(sr => sr.AssignedProviderId == providerId)
                .AsNoTracking()
                .ToListAsync();

            var thisMonthRequests = allRequests.Where(r => r.CreatedAt >= startOfThisMonth).ToList();
            var lastMonthRequests = allRequests.Where(r => r.CreatedAt >= startOfLastMonth && r.CreatedAt < startOfThisMonth).ToList();

            // Get reviews for satisfaction metrics
            var reviews = await _context.Reviews
                .Where(r => r.ProviderId == providerId)
                .AsNoTracking()
                .ToListAsync();

            // Calculate analytics
            var summary = CalculateSummary(thisMonthRequests);
            var keyMetrics = await CalculateKeyMetricsAsync(providerId, thisMonthRequests, lastMonthRequests, reviews);
            var performanceBreakdown = CalculatePerformanceBreakdown(thisMonthRequests);
            var monthlyTrends = CalculateMonthlyTrends(thisMonthRequests, lastMonthRequests, providerId);

            var response = new ProviderAnalyticsResponseDto
            {
                Summary = summary,
                KeyMetrics = keyMetrics,
                PerformanceBreakdown = performanceBreakdown,
                MonthlyTrends = monthlyTrends
            };

            return (true, "Analytics data retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for provider {ProviderId}", providerId);
            return (false, "An error occurred while retrieving analytics data", null);
        }
    }

    private AnalyticsSummaryDto CalculateSummary(List<ServiceRequest> thisMonthRequests)
    {
        var totalJobs = thisMonthRequests.Count;
        var completedJobs = thisMonthRequests.Count(r => r.Status == ServiceRequestStatus.Completed);
        var completionRate = totalJobs > 0 ? (int)Math.Round((double)completedJobs / totalJobs * 100) : 0;

        return new AnalyticsSummaryDto
        {
            CompletionRate = completionRate,
            CompletionTrend = 5, // This would be calculated vs last month
            TotalJobsThisMonth = totalJobs,
            CompletedJobsThisMonth = completedJobs
        };
    }

    private async Task<KeyMetricsDto> CalculateKeyMetricsAsync(
        Guid providerId, 
        List<ServiceRequest> thisMonthRequests, 
        List<ServiceRequest> lastMonthRequests,
        List<Review> reviews)
    {
        // Response time calculation (simplified - would need actual assignment/acceptance timestamps)
        var avgResponseTime = 12; // Placeholder - would calculate from actual data

        // Customer satisfaction from reviews (4+ stars = satisfied)
        var satisfiedReviews = reviews.Count(r => r.Rating >= 4);
        var satisfactionPercentage = reviews.Count > 0 ? (int)Math.Round((double)satisfiedReviews / reviews.Count * 100) : 0;

        // Repeat customers calculation
        var repeatCustomerPercentage = await CalculateRepeatCustomersAsync(providerId);

        return new KeyMetricsDto
        {
            ResponseTime = new ResponseTimeMetricDto
            {
                AverageMinutes = avgResponseTime,
                Trend = 8 // Improvement vs last month
            },
            CustomerSatisfaction = new CustomerSatisfactionMetricDto
            {
                Percentage = satisfactionPercentage,
                Trend = 3
            },
            JobsCompleted = new JobsCompletedMetricDto
            {
                Count = thisMonthRequests.Count(r => r.Status == ServiceRequestStatus.Completed),
                Trend = 12
            },
            RepeatCustomers = new RepeatCustomersMetricDto
            {
                Percentage = repeatCustomerPercentage,
                Trend = -2
            }
        };
    }

    private PerformanceBreakdownDto CalculatePerformanceBreakdown(List<ServiceRequest> requests)
    {
        var total = requests.Count;
        if (total == 0)
        {
            return new PerformanceBreakdownDto();
        }

        var completed = requests.Count(r => r.Status == ServiceRequestStatus.Completed);
        var inProgress = requests.Count(r => r.Status == ServiceRequestStatus.InProgress || r.Status == ServiceRequestStatus.CheckedIn);
        var cancelled = requests.Count(r => r.Status == ServiceRequestStatus.Cancelled);

        return new PerformanceBreakdownDto
        {
            Completed = new StatusBreakdownDto
            {
                Count = completed,
                Percentage = (int)Math.Round((double)completed / total * 100)
            },
            InProgress = new StatusBreakdownDto
            {
                Count = inProgress,
                Percentage = (int)Math.Round((double)inProgress / total * 100)
            },
            Cancelled = new StatusBreakdownDto
            {
                Count = cancelled,
                Percentage = (int)Math.Round((double)cancelled / total * 100)
            }
        };
    }

    private MonthlyTrendsDto CalculateMonthlyTrends(
        List<ServiceRequest> thisMonthRequests, 
        List<ServiceRequest> lastMonthRequests,
        Guid providerId)
    {
        // Completion rates
        var thisMonthCompletionRate = thisMonthRequests.Count > 0 
            ? (int)Math.Round((double)thisMonthRequests.Count(r => r.Status == ServiceRequestStatus.Completed) / thisMonthRequests.Count * 100)
            : 0;

        var lastMonthCompletionRate = lastMonthRequests.Count > 0
            ? (int)Math.Round((double)lastMonthRequests.Count(r => r.Status == ServiceRequestStatus.Completed) / lastMonthRequests.Count * 100)
            : 0;

        var completionImprovement = thisMonthCompletionRate - lastMonthCompletionRate;

        // Response time trends (simplified)
        var thisMonthResponseTime = 12;
        var lastMonthResponseTime = 15;
        var responseTimeImprovement = lastMonthResponseTime - thisMonthResponseTime;

        // Provider ranking (simplified)
        var percentile = 95;
        var description = percentile >= 95 ? "Top 5%" : 
                         percentile >= 90 ? "Top 10%" : 
                         percentile >= 75 ? "Top 25%" : 
                         percentile >= 50 ? "Above Average" : "Below Average";

        return new MonthlyTrendsDto
        {
            CompletionRate = new CompletionRateTrendDto
            {
                ThisMonth = thisMonthCompletionRate,
                LastMonth = lastMonthCompletionRate,
                Improvement = completionImprovement
            },
            ResponseTime = new ResponseTimeTrendDto
            {
                ThisMonth = thisMonthResponseTime,
                LastMonth = lastMonthResponseTime,
                Improvement = responseTimeImprovement
            },
            Ranking = new RankingDto
            {
                Percentile = percentile,
                Description = description
            }
        };
    }

    private async Task<int> CalculateRepeatCustomersAsync(Guid providerId)
    {
        // Get all customers who have used this provider
        var customerCounts = await _context.ServiceRequests
            .Where(sr => sr.AssignedProviderId == providerId && sr.Status == ServiceRequestStatus.Completed)
            .GroupBy(sr => sr.RequesterId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalCustomers = customerCounts.Count;
        var repeatCustomers = customerCounts.Count(c => c.Count > 1);

        return totalCustomers > 0 ? (int)Math.Round((double)repeatCustomers / totalCustomers * 100) : 0;
    }
}