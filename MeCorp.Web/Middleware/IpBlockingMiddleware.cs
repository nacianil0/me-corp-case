using MeCorp.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MeCorp.Web.Middleware;

public class IpBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpBlockingMiddleware> _logger;
    private const string BlockedIpCachePrefix = "blocked_ip_";
    private const int MaxFailedAttempts = 10;
    private const int FailedAttemptWindowMinutes = 15;
    private const int BlockDurationMinutes = 30;

    public IpBlockingMiddleware(RequestDelegate next, ILogger<IpBlockingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        string? ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress))
        {
            await _next(context);
            return;
        }

        string cacheKey = $"{BlockedIpCachePrefix}{ipAddress}";

        if (cache.TryGetValue(cacheKey, out bool isBlocked) && isBlocked)
        {
            _logger.LogWarning("Blocked IP attempted access: {IpAddress}", ipAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Your IP address has been temporarily blocked due to too many failed login attempts.");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        DateTime cutoffTime = DateTime.UtcNow.AddMinutes(-FailedAttemptWindowMinutes);
        int failedAttempts = await dbContext.LoginAttempts
            .Where(l => l.IpAddress == ipAddress && !l.IsSuccessful && l.AttemptTime >= cutoffTime)
            .CountAsync();

        if (failedAttempts >= MaxFailedAttempts)
        {
            cache.Set(cacheKey, true, TimeSpan.FromMinutes(BlockDurationMinutes));
            _logger.LogWarning("IP blocked due to {Count} failed attempts: {IpAddress}", failedAttempts, ipAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Your IP address has been temporarily blocked due to too many failed login attempts.");
            return;
        }

        await _next(context);
    }
}

public static class IpBlockingMiddlewareExtensions
{
    public static IApplicationBuilder UseIpBlocking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IpBlockingMiddleware>();
    }
}

