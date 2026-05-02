using System.Diagnostics;
using System.Security.Claims;

namespace CurrencyConversionPlatform.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var method = context.Request.Method;
        var path = context.Request.Path;

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms | IP={ClientIp} | ClientId={ClientId} | CorrelationId={CorrelationId}",
                method, path, context.Response.StatusCode, sw.ElapsedMilliseconds,
                clientIp, clientId, correlationId);
        }
    }
}
