using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ApiConsulta.Presentation.Middlewares;

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
        var request = context.Request;

        _logger.LogInformation("Incoming request: {Method} {Path}", request.Method, request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {ElapsedMs}ms",
                request.Method, request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}