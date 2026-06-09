using Business.Abstract;
using Business.Concrete;

namespace WebAPI.Middlewares;

/// <summary>
/// Tracks 404 responses per IP in a 5-minute window. When a single IP triggers
/// more than 30 consecutive 404s the first crossing is logged as "enumeration_detected".
/// </summary>
public class EnumerationDetectionMiddleware
{
    private readonly RequestDelegate _next;
    private const int Threshold = 30;

    public EnumerationDetectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == 404 && context.Request.Path.Value?.Contains("getbyid") == true)
        {
            var secSvc = context.RequestServices.GetService<ISecurityEventService>();
            var manager = secSvc as SecurityEventManager;
            if (manager != null)
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (manager.TrackNotFound(ip, Threshold))
                {
                    manager.LogEvent("enumeration_detected", "high", ip,
                        context.Request.Path.Value, $"30+ consecutive 404s in 5-minute window");
                }
            }
        }
    }
}
