using Business.Abstract;
using Core.Utilities.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace WebAPI.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ISystemSettingsService systemSettingsService, ICapabilityResolver capabilityResolver)
    {
        var settingsResult = systemSettingsService.Get();

        if (settingsResult.Success && settingsResult.Data.IsMaintenanceMode)
        {
            bool isAuthPath = httpContext.Request.Path.Equals("/api/user/login", StringComparison.OrdinalIgnoreCase);

            var userIdClaim = httpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            bool isAdmin = userIdClaim != null
                && int.TryParse(userIdClaim.Value, out int uid)
                && capabilityResolver.Allows(uid, "admin.system_access");

            if (!isAuthPath && !isAdmin)
            {
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                httpContext.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = settingsResult.Data.MaintenanceMessage ?? "Sistem bakımdadır. Lütfen daha sonra tekrar deneyin."
                };

                await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }
        }

        await _next(httpContext);
    }
}
