using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Services;

/// <summary>
/// Service for calculating provider earnings
/// </summary>
public class EarningsService : IEarningsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EarningsService> _logger;

    public EarningsService(ApplicationDbContext context, ILogger<EarningsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ProviderEarningsResponseDto? Data)> GetProviderEarningsAsync(
        Guid providerId, 
        string? period = null)
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
            var today = now.Date;

            // Get all completed service requests for this provider
            var completedRequests = await _context.ServiceRequests
                .Where(sr => sr.AssignedProviderId == providerId && sr.Status == ServiceRequestStatus.Completed)
                .Include(sr => sr.Quotes.Where(q => q.ProviderId == providerId))
                .AsNoTracking()
                .ToListAsync();

            // Calculate earnings summary
            var summary = CalculateEarningsSummary(completedRequests, today);

            // Calculate period data with growth
            var periodData = CalculatePeriodData(completedRequests, today);

            // Calculate daily breakdown (last 7 days)
            var dailyBreakdown = CalculateDailyBreakdown(completedRequests, today);

            var response = new ProviderEarningsResponseDto
            {
                Summary = summary,
                PeriodData = periodData,
                DailyBreakdown = dailyBreakdown
            };

            return (true, "Earnings data retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving earnings for provider {ProviderId}", providerId);
            return (false, "An error occurred while retrieving earnings data", null);
        }
    }

    private EarningsSummaryDto CalculateEarningsSummary(List<ServiceRequest> requests, DateTime today)
    {
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfQuarter = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
        var startOfYear = new DateTime(today.Year, 1, 1);

        return new EarningsSummaryDto
        {
            TodayEarnings = GetEarningsForPeriod(requests, today, today.AddDays(1)),
            WeekEarnings = GetEarningsForPeriod(requests, startOfWeek, startOfWeek.AddDays(7)),
            MonthEarnings = GetEarningsForPeriod(requests, startOfMonth, startOfMonth.AddMonths(1)),
            QuarterEarnings = GetEarningsForPeriod(requests, startOfQuarter, startOfQuarter.AddMonths(3)),
            YearEarnings = GetEarningsForPeriod(requests, startOfYear, startOfYear.AddYears(1))
        };
    }

    private PeriodDataDto CalculatePeriodData(List<ServiceRequest> requests, DateTime today)
    {
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfQuarter = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
        var startOfYear = new DateTime(today.Year, 1, 1);

        return new PeriodDataDto
        {
            Today = GetPeriodDetail(requests, today, today.AddDays(1), today.AddDays(-1), today),
            ThisWeek = GetPeriodDetail(requests, startOfWeek, startOfWeek.AddDays(7), startOfWeek.AddDays(-7), startOfWeek),
            LastWeek = GetPeriodDetail(requests, startOfWeek.AddDays(-7), startOfWeek, startOfWeek.AddDays(-14), startOfWeek.AddDays(-7)),
            ThisMonth = GetPeriodDetail(requests, startOfMonth, startOfMonth.AddMonths(1), startOfMonth.AddMonths(-1), startOfMonth),
            ThisQuarter = GetPeriodDetail(requests, startOfQuarter, startOfQuarter.AddMonths(3), startOfQuarter.AddMonths(-3), startOfQuarter),
            ThisYear = GetPeriodDetail(requests, startOfYear, startOfYear.AddYears(1), startOfYear.AddYears(-1), startOfYear)
        };
    }

    private List<DailyBreakdownDto> CalculateDailyBreakdown(List<ServiceRequest> requests, DateTime today)
    {
        var breakdown = new List<DailyBreakdownDto>();
        
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayRequests = requests.Where(r => r.UpdatedAt?.Date == date).ToList();
            
            breakdown.Add(new DailyBreakdownDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                DayOfWeek = date.ToString("ddd"),
                Amount = GetEarningsFromRequests(dayRequests),
                JobsCompleted = dayRequests.Count
            });
        }

        return breakdown;
    }

    private PeriodDetailDto GetPeriodDetail(List<ServiceRequest> requests, DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd)
    {
        var currentRequests = requests.Where(r => r.UpdatedAt >= start && r.UpdatedAt < end).ToList();
        var previousRequests = requests.Where(r => r.UpdatedAt >= prevStart && r.UpdatedAt < prevEnd).ToList();

        var currentAmount = GetEarningsFromRequests(currentRequests);
        var previousAmount = GetEarningsFromRequests(previousRequests);

        var growthPercentage = previousAmount > 0 
            ? ((currentAmount - previousAmount) / previousAmount) * 100 
            : 0;

        return new PeriodDetailDto
        {
            Amount = currentAmount,
            JobsCompleted = currentRequests.Count,
            GrowthPercentage = Math.Round(growthPercentage, 1)
        };
    }

    private decimal GetEarningsForPeriod(List<ServiceRequest> requests, DateTime start, DateTime end)
    {
        var periodRequests = requests.Where(r => r.UpdatedAt >= start && r.UpdatedAt < end).ToList();
        return GetEarningsFromRequests(periodRequests);
    }

    private decimal GetEarningsFromRequests(List<ServiceRequest> requests)
    {
        decimal totalEarnings = 0;

        foreach (var request in requests)
        {
            // Get the accepted quote for this request
            var acceptedQuote = request.Quotes.FirstOrDefault();
            if (acceptedQuote != null)
            {
                totalEarnings += acceptedQuote.Price;
            }
        }

        return totalEarnings;
    }
}
