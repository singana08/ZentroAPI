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
    
    private static void WriteToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log");
            var logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
            File.AppendAllText(logPath, logMessage);
        }
        catch
        {
            // Ignore file write errors
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Log request with full details
        var fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var logMessage = $"[{requestId}] {context.Request.Method} {fullUrl} - Started";
        _logger.LogInformation(logMessage);
        WriteToFile($"API_REQUEST: {logMessage}");

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
            var errorMsg = $"[{requestId}] EXCEPTION: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            WriteToFile($"API_ERROR: {errorMsg}");
            WriteToFile($"STACK_TRACE: {ex.StackTrace}");
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            
            // Log response
            var responseMsg = $"[{requestId}] {context.Request.Method} {context.Request.Path} - {context.Response.StatusCode} - {duration.TotalMilliseconds:F2}ms";
            _logger.LogInformation(responseMsg);
            WriteToFile($"API_RESPONSE: {responseMsg}");

            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}