using System.Collections.Concurrent;
using System.Net;

namespace ZentroAPI.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _requestLimit = 100; // requests per minute
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var now = DateTime.UtcNow;
        
        var clientInfo = _clients.AddOrUpdate(clientId, 
            new ClientRequestInfo { LastRequest = now, RequestCount = 1 },
            (key, existing) =>
            {
                if (now - existing.LastRequest > _timeWindow)
                {
                    existing.RequestCount = 1;
                    existing.LastRequest = now;
                }
                else
                {
                    existing.RequestCount++;
                }
                return existing;
            });

        if (clientInfo.RequestCount > _requestLimit)
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = _timeWindow.TotalSeconds.ToString();
            
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = _requestLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (_requestLimit - clientInfo.RequestCount).ToString();
        
        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Use user ID if authenticated, otherwise use IP
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
            return $"user_{userId}";
            
        return $"ip_{context.Connection.RemoteIpAddress}";
    }

    private class ClientRequestInfo
    {
        public DateTime LastRequest { get; set; }
        public int RequestCount { get; set; }
    }
}