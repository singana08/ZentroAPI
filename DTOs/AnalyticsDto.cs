namespace HaluluAPI.DTOs;

/// <summary>
/// Provider analytics response DTO
/// </summary>
public class ProviderAnalyticsResponseDto
{
    public AnalyticsSummaryDto Summary { get; set; } = new();
    public KeyMetricsDto KeyMetrics { get; set; } = new();
    public PerformanceBreakdownDto PerformanceBreakdown { get; set; } = new();
    public MonthlyTrendsDto MonthlyTrends { get; set; } = new();
}

/// <summary>
/// Analytics summary data
/// </summary>
public class AnalyticsSummaryDto
{
    public int CompletionRate { get; set; }
    public int CompletionTrend { get; set; }
    public int TotalJobsThisMonth { get; set; }
    public int CompletedJobsThisMonth { get; set; }
}

/// <summary>
/// Key performance metrics
/// </summary>
public class KeyMetricsDto
{
    public ResponseTimeMetricDto ResponseTime { get; set; } = new();
    public CustomerSatisfactionMetricDto CustomerSatisfaction { get; set; } = new();
    public JobsCompletedMetricDto JobsCompleted { get; set; } = new();
    public RepeatCustomersMetricDto RepeatCustomers { get; set; } = new();
}

/// <summary>
/// Response time metric
/// </summary>
public class ResponseTimeMetricDto
{
    public int AverageMinutes { get; set; }
    public int Trend { get; set; }
}

/// <summary>
/// Customer satisfaction metric
/// </summary>
public class CustomerSatisfactionMetricDto
{
    public int Percentage { get; set; }
    public int Trend { get; set; }
}

/// <summary>
/// Jobs completed metric
/// </summary>
public class JobsCompletedMetricDto
{
    public int Count { get; set; }
    public int Trend { get; set; }
}

/// <summary>
/// Repeat customers metric
/// </summary>
public class RepeatCustomersMetricDto
{
    public int Percentage { get; set; }
    public int Trend { get; set; }
}

/// <summary>
/// Performance breakdown by status
/// </summary>
public class PerformanceBreakdownDto
{
    public StatusBreakdownDto Completed { get; set; } = new();
    public StatusBreakdownDto InProgress { get; set; } = new();
    public StatusBreakdownDto Cancelled { get; set; } = new();
}

/// <summary>
/// Individual status breakdown
/// </summary>
public class StatusBreakdownDto
{
    public int Count { get; set; }
    public int Percentage { get; set; }
}

/// <summary>
/// Monthly trends comparison
/// </summary>
public class MonthlyTrendsDto
{
    public CompletionRateTrendDto CompletionRate { get; set; } = new();
    public ResponseTimeTrendDto ResponseTime { get; set; } = new();
    public RankingDto Ranking { get; set; } = new();
}

/// <summary>
/// Completion rate trend
/// </summary>
public class CompletionRateTrendDto
{
    public int ThisMonth { get; set; }
    public int LastMonth { get; set; }
    public int Improvement { get; set; }
}

/// <summary>
/// Response time trend
/// </summary>
public class ResponseTimeTrendDto
{
    public int ThisMonth { get; set; }
    public int LastMonth { get; set; }
    public int Improvement { get; set; }
}

/// <summary>
/// Provider ranking data
/// </summary>
public class RankingDto
{
    public int Percentile { get; set; }
    public string Description { get; set; } = string.Empty;
}