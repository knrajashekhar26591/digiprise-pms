using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/problem+json";

        var (status, title) = ex switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access Denied"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid Operation"),
            ArgumentException or ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid Input"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        context.Response.StatusCode = (int)status;

        var problem = new ProblemDetails
        {
            Title = title,
            Status = (int)status,
            Detail = status == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : ex.Message,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}

// ── Tenant Resolution Middleware ──────────────────────────────────────
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve tenant from subdomain or header
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.').FirstOrDefault() ?? "default";
        context.Items["TenantSubdomain"] = subdomain;
        await _next(context);
    }
}

// ── Request Logging Middleware ─────────────────────────────────────────
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var level = context.Response.StatusCode >= 500 ? LogLevel.Error :
                    context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(level, "{Method} {Path} {StatusCode} {ElapsedMs}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
