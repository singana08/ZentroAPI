using System.Diagnostics;

namespace ZentroAPI.Middleware;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;

    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var responseTime = stopwatch.ElapsedMilliseconds;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var statusCode = context.Response.StatusCode;
            
            // Log slow requests (>1000ms)
            if (responseTime > 1000)
            {
                _logger.LogWarning("Slow request: {Method} {Path} took {ResponseTime}ms - Status: {StatusCode}",
                    method, path, responseTime, statusCode);
            }
            else
            {
                _logger.LogInformation("Request: {Method} {Path} - {ResponseTime}ms - Status: {StatusCode}",
                    method, path, responseTime, statusCode);
            }
            
            // Add response time header
            context.Response.Headers.Add("X-Response-Time", $"{responseTime}ms");
        }
    }
}