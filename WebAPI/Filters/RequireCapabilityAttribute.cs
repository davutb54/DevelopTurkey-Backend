using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace WebAPI.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireCapabilityAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string CapabilityCode { get; }

    public RequireCapabilityAttribute(string capabilityCode)
    {
        CapabilityCode = capabilityCode;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        var policy = context.HttpContext.RequestServices.GetService<ICapabilityPolicy>();
        if (policy == null)
        {
            context.Result = new StatusCodeResult(500);
            return Task.CompletedTask;
        }

        var clientContext = context.HttpContext.RequestServices.GetService<IClientContext>();
        var ctx = new CapabilityRequestContext(
            InstitutionId: clientContext?.GetInstitutionId());

        if (!policy.Allows(CapabilityCode, ctx))
        {
            // Log 403 capability denial as a security event
            var secSvc = context.HttpContext.RequestServices.GetService<ISecurityEventService>();
            if (secSvc != null)
            {
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path = context.HttpContext.Request.Path.Value;
                var userIdStr = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userId = int.TryParse(userIdStr, out var uid) ? uid : null;
                secSvc.LogEvent("capability_denied", "medium", ip, path,
                    $"Required: {CapabilityCode}", userId);
            }

            context.Result = new ObjectResult(new
            {
                success = false,
                message = $"Yetersiz yetki: '{CapabilityCode}' gerekli.",
                requiredCapability = CapabilityCode,
            })
            { StatusCode = 403 };
        }

        return Task.CompletedTask;
    }
}
