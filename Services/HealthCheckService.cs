using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ZentroAPI.Data;

namespace ZentroAPI.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthCheckService> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthCheckService(ApplicationDbContext context, ILogger<HealthCheckService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckDatabaseAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            stopwatch.Stop();
            
            return new HealthCheckResult
            {
                IsHealthy = true,
                Status = "Healthy",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");
            
            return new HealthCheckResult
            {
                IsHealthy = false,
                Status = "Unhealthy",
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<HealthCheckResult> CheckExternalServicesAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Simulate external service check
            await Task.Delay(50);
            stopwatch.Stop();
            
            return new HealthCheckResult
            {
                IsHealthy = true,
                Status = "Healthy",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckResult
            {
                IsHealthy = false,
                Status = "Unhealthy",
                ResponseTime = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        var checks = new Dictionary<string, HealthCheckResult>
        {
            ["database"] = await CheckDatabaseAsync(),
            ["external_services"] = await CheckExternalServicesAsync()
        };

        var overallHealthy = checks.Values.All(c => c.IsHealthy);
        
        return new SystemHealthDto
        {
            Status = overallHealthy ? "Healthy" : "Unhealthy",
            Timestamp = DateTime.UtcNow,
            Checks = checks,
            Uptime = DateTime.UtcNow - _startTime,
            MemoryUsage = GC.GetTotalMemory(false)
        };
    }
}