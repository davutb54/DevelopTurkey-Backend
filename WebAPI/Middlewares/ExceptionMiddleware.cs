using System.Net;
using System.Text.Json;
using Business.Abstract;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogService logService)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            logService.LogCritical("System", "UnhandledException", $"Sistem Hatası: {ex.Message}", ex.StackTrace);

            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var result = new ErrorResult($"Sunucu Hatası: {exception.Message}");

        var json = JsonSerializer.Serialize(result);
        return httpContext.Response.WriteAsync(json);
    }
}