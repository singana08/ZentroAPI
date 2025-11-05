using System.Diagnostics;
using System.Text;

namespace HaluluAPI.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Log request
            await LogRequestAsync(context);

            // Copy response stream for logging
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call next middleware
            await _next(context);

            // Log response
            stopwatch.Stop();
            await LogResponseAsync(context, stopwatch.ElapsedMilliseconds);

            // Copy response to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;
        var sb = new StringBuilder();

        sb.AppendLine($"HTTP {request.Method} {request.Path}{request.QueryString}");
        sb.AppendLine($"Host: {request.Host}");

        foreach (var header in request.Headers)
        {
            // Don't log authorization headers
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine($"{header.Key}: {string.Join(", ", (IEnumerable<string>)header.Value)}");
        }

        if (request.ContentLength > 0)
        {
            request.EnableBuffering();
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;

            if (!string.IsNullOrEmpty(body))
            {
                // Don't log sensitive data
                sb.AppendLine($"Body: {body.Substring(0, Math.Min(body.Length, 500))}");
            }
        }

        _logger.LogInformation($"Request: {sb}");
    }

    private async Task LogResponseAsync(HttpContext context, long elapsedMilliseconds)
    {
        var response = context.Response;
        var sb = new StringBuilder();

        sb.AppendLine($"HTTP {response.StatusCode}");
        sb.AppendLine($"Duration: {elapsedMilliseconds}ms");

        foreach (var header in response.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", (IEnumerable<string>)header.Value)}");
        }

        if (response.Body.CanSeek)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            if (!string.IsNullOrEmpty(body))
            {
                sb.AppendLine($"Body: {body.Substring(0, Math.Min(body.Length, 500))}");
            }
        }

        _logger.LogInformation($"Response: {sb}");
    }
}