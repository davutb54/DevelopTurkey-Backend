using System.Net;
using System.Text.Json;
using Business.Abstract;
using Core.CrossCuttingConcerns.Logging;
using Core.Utilities.Authorization;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WebAPI.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogService logService, IExceptionFileLogger exceptionFileLogger)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            var request = httpContext.Request;
            var endpoint = $"{request.Method} {request.Path}{request.QueryString}";

            // Dinamik Exception Analyzer mimarisi gelene kadar boş liste döndürüyoruz.
            var solutions = new List<string>();

            int? userId = null;
            var nameIdentifier = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(nameIdentifier, out int parsedId)) userId = parsedId;

            var detail = new ExceptionLogDetail
            {
                ExceptionType = ex.GetType().Name,
                Endpoint = endpoint,
                Method = request.Method,
                ClientIp = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserId = userId,
                TraceId = httpContext.TraceIdentifier,
                StackTrace = ex.StackTrace,
                SuggestedSolutions = solutions
            };

            string jsonDetails = System.Text.Json.JsonSerializer.Serialize(detail, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            // 1) Sunucu dosyasına yaz (DB çökmüş olsa bile log kalsın)
            var fileEntry = new ExceptionLogFileEntry(
                TimestampUtc: DateTimeOffset.UtcNow,
                Environment: _env.EnvironmentName,
                MachineName: Environment.MachineName,
                TraceId: httpContext.TraceIdentifier,
                UserId: userId?.ToString(),
                Endpoint: endpoint,
                Method: request.Method,
                ClientIp: httpContext.Connection.RemoteIpAddress?.ToString(),
                ExceptionType: ex.GetType().Name,
                ExceptionMessage: ex.Message,
                ExceptionToString: ex.ToString(),
                Detail: detail
            );

            await exceptionFileLogger.TryLogAsync(fileEntry, CancellationToken.None);

            // 2) Mevcut DB loglamayı da koru
            try
            {
                logService.LogCritical("System", "UnhandledException", $"Sistem Hatası: {ex.Message}", jsonDetails);
            }
            catch
            {
                // DB log'u patlarsa request'in hata cevabını engellemesin.
            }

            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.ContentType = "application/json";

        if (exception is CapabilityDeniedException capEx)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            var capResult = new
            {
                success = false,
                message = capEx.Message,
                requiredCapability = capEx.RequiredCapability,
            };
            return httpContext.Response.WriteAsync(JsonSerializer.Serialize(capResult));
        }

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        string message = _env.IsDevelopment()
            ? $"Sunucu Hatası: {exception.Message}"
            : "Sunucuda beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.";

        var result = new ErrorResult(message);
        var json = JsonSerializer.Serialize(result);
        return httpContext.Response.WriteAsync(json);
    }
}