namespace ZentroAPI.Services;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckDatabaseAsync();
    Task<HealthCheckResult> CheckExternalServicesAsync();
    Task<SystemHealthDto> GetSystemHealthAsync();
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SystemHealthDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; } = new();
    public TimeSpan Uptime { get; set; }
    public long MemoryUsage { get; set; }
}