using Core.CrossCuttingConcerns.Monitoring;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Middlewares;

public class SystemMonitorMiddleware
{
    private readonly RequestDelegate _next;

    public SystemMonitorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISystemMonitor systemMonitor)
    {
        var sw = Stopwatch.StartNew();
        bool isError = false;

        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User.Identity?.IsAuthenticated == true 
            ? context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value 
            : null;
            
        string clientIdentifier = userId != null ? $"User_{userId}" : $"IP_{clientIp}";

        try
        {
            await _next(context);
            if (context.Response.StatusCode >= 500)
            {
                isError = true;
            }
        }
        catch
        {
            isError = true;
            throw;
        }
        finally
        {
            sw.Stop();
            systemMonitor.RecordRequest(sw.Elapsed.TotalMilliseconds, isError, clientIdentifier);
        }
    }
}
