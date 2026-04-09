using System.Net;
using System.Text.Json;
using Business.Abstract;
using Core.CrossCuttingConcerns.Logging;
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

    public async Task InvokeAsync(HttpContext httpContext, ILogService logService)
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
                StackTrace = ex.StackTrace,
                SuggestedSolutions = solutions
            };

            string jsonDetails = System.Text.Json.JsonSerializer.Serialize(detail, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            logService.LogCritical("System", "UnhandledException", $"Sistem Hatası: {ex.Message}", jsonDetails);

            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        string message = _env.IsDevelopment()
            ? $"Sunucu Hatası: {exception.Message}"
            : "Sunucuda beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.";

        var result = new ErrorResult(message);
        var json = JsonSerializer.Serialize(result);
        return httpContext.Response.WriteAsync(json);
    }
}