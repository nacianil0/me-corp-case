using System.Net;

namespace MeCorp.Web.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred. Path: {Path}, IP: {IpAddress}",
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString());

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        if (context.Request.Headers.Accept.Any(h => h?.Contains("text/html") == true))
        {
            context.Response.Redirect("/Home/Error");
        }
        else
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("An unexpected error occurred.");
        }
    }
}

public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}

