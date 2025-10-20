using System.Diagnostics;

namespace ApiPedidos.Presentation.Middlewares;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var method = context.Request.Method;

        _logger.LogInformation("📥 Incoming request: {Method} {Path}", method, requestPath);

        try
        {
            await _next(context);
            stopwatch.Stop();

            _logger.LogInformation("✅ Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {ElapsedMs}ms",
                method, requestPath, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _logger.LogError("❌ Request failed: {Method} {Path} - Duration: {ElapsedMs}ms",
                method, requestPath, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}