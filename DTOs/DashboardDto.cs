namespace ZentroAPI.DTOs;

/// <summary>
/// Provider dashboard summary response DTO
/// </summary>
public class ProviderDashboardResponseDto
{
    public decimal TodayEarnings { get; set; }
    public int ActiveRequests { get; set; }
    public decimal Rating { get; set; }
    public int CompletionRate { get; set; }
    public int NotificationCount { get; set; }
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Requester dashboard summary response DTO
/// </summary>
public class RequesterDashboardResponseDto
{
    public string UserName { get; set; } = string.Empty;
    public int ActiveRequests { get; set; }
    public int CompletedServices { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal SavedAmount { get; set; }
    public List<ScheduledServiceDto> ScheduledServices { get; set; } = new();
}

/// <summary>
/// Scheduled service DTO for dashboard
/// </summary>
public class ScheduledServiceDto
{
    public Guid RequestId { get; set; }
    public Guid? ProviderId { get; set; }
    public string MainCategory { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string? Time { get; set; }
    public string Status { get; set; } = string.Empty;
}
