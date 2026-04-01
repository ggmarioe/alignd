using System.Text.Json;

namespace Alignd.API.Middleware;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode  = 500;
            ctx.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new
            {
                errors = new[] { new { code = "server.error", message = "An unexpected error occurred. Please try again." } }
            });

            await ctx.Response.WriteAsync(body);
        }
    }
}
