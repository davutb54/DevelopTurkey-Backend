using Core.Utilities.Context;

namespace WebAPI.Middlewares;

/// <summary>
/// Her HTTP isteği başında ITenantProvider'ı resolve ederek TenantScopeContext'i kurar.
/// UseAuthentication / UseAuthorization'dan SONRA pipeline'a eklenmeli;
/// böylece kullanıcı claim'leri okunabilir.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // ITenantProvider'ı resolve etmek constructor side-effect'i (TenantScopeContext.Set)
        // tetikler. Gerçek iş burada değil, constructor'da yapılır.
        _ = tenantProvider;
        return _next(context);
    }
}
