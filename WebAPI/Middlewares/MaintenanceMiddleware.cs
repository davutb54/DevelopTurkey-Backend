using Business.Abstract;
using System.Text.Json;

namespace WebAPI.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ISystemSettingsService systemSettingsService)
    {
        var settingsResult = systemSettingsService.Get();

        if (settingsResult.Success && settingsResult.Data.IsMaintenanceMode)
        {
            bool isAuthPath = httpContext.Request.Path.Equals("/api/user/login", StringComparison.OrdinalIgnoreCase);

            bool isAdmin = httpContext.User.IsInRole("Admin");

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
