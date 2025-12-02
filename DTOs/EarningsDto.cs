namespace ZentroAPI.DTOs;

/// <summary>
/// Provider earnings response DTO
/// </summary>
public class ProviderEarningsResponseDto
{
    public EarningsSummaryDto Summary { get; set; } = new();
    public PeriodDataDto PeriodData { get; set; } = new();
    public List<DailyBreakdownDto> DailyBreakdown { get; set; } = new();
}

/// <summary>
/// Earnings summary for all periods
/// </summary>
public class EarningsSummaryDto
{
    public decimal TodayEarnings { get; set; }
    public decimal WeekEarnings { get; set; }
    public decimal MonthEarnings { get; set; }
    public decimal QuarterEarnings { get; set; }
    public decimal YearEarnings { get; set; }
}

/// <summary>
/// Period-based earnings data with growth
/// </summary>
public class PeriodDataDto
{
    public PeriodDetailDto Today { get; set; } = new();
    public PeriodDetailDto ThisWeek { get; set; } = new();
    public PeriodDetailDto LastWeek { get; set; } = new();
    public PeriodDetailDto ThisMonth { get; set; } = new();
    public PeriodDetailDto ThisQuarter { get; set; } = new();
    public PeriodDetailDto ThisYear { get; set; } = new();
}

/// <summary>
/// Individual period details
/// </summary>
public class PeriodDetailDto
{
    public decimal Amount { get; set; }
    public int JobsCompleted { get; set; }
    public decimal GrowthPercentage { get; set; }
}

/// <summary>
/// Daily earnings breakdown
/// </summary>
public class DailyBreakdownDto
{
    public string Date { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int JobsCompleted { get; set; }
}
