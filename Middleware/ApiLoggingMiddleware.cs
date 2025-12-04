using System.Text;

namespace ZentroAPI.Middleware;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Log request
        _logger.LogInformation("[{RequestId}] {Method} {Path} - Started", 
            requestId, context.Request.Method, context.Request.Path);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Exception: {Message}", requestId, ex.Message);
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            
            // Log response
            _logger.LogInformation("[{RequestId}] {Method} {Path} - {StatusCode} - {Duration}ms", 
                requestId, context.Request.Method, context.Request.Path, 
                context.Response.StatusCode, duration.TotalMilliseconds);

            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}