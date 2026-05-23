using Core.Utilities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

        if (!policy.Allows(CapabilityCode))
        {
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
