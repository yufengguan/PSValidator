using System.Security.Cryptography;
using System.Text;
using Serilog.Context;

namespace PromoStandards.Validator.Api.Middleware;

public class AnalyticsMiddleware
{
    private readonly RequestDelegate _next;
    // Salt rotates daily to prevent long-term tracking of IPs
    private static string _dailySalt = Guid.NewGuid().ToString();
    private static DateTime _saltDate = DateTime.UtcNow.Date;

    public AnalyticsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Session ID (from Client Header or New)
        var sessionId = context.Request.Headers["X-Session-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // 2. Derive Privacy-Safe User ID from IP
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var date = DateTime.UtcNow.Date;
        
        // Rotate salt if day has changed
        if (date > _saltDate)
        {
            _dailySalt = Guid.NewGuid().ToString();
            _saltDate = date;
        }

        var userId = ComputeHash($"{ipAddress}-{_dailySalt}");

        // 3. Push to LogContext
        using (LogContext.PushProperty("SessionId", sessionId))
        using (LogContext.PushProperty("RefUserId", userId)) // "Ref" for Reference/Referrer/Refined - distinct from actual DB UserIds
        {
            await _next(context);
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // Keep it short
    }
}
