namespace HaluluAPI.DTOs;

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