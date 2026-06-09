using Core.Utilities.Authorization;
using Core.Utilities.Context;
using System.Security.Claims;

namespace WebAPI.Context;

/// <summary>
/// HTTP request başına bir kez resolve edilen scoped servis.
/// Constructor'da TenantScopeContext'i ayarlar; böylece aynı async akışındaki
/// tüm `new DevelopTurkeyContext()` çağrıları (EfEntityRepositoryBase dahil)
/// doğru tenant snapshot'ını kullanır.
/// </summary>
public sealed class WebTenantProvider : ITenantProvider
{
    public int?  InstitutionId { get; }
    public bool  IsGlobalAdmin { get; }

    public WebTenantProvider(IHttpContextAccessor accessor, ICapabilityResolver resolver)
    {
        var httpContext = accessor.HttpContext;

        if (httpContext == null)
        {
            // Arka plan servisi / seeder — tüm filtreleri atla
            IsGlobalAdmin = true;
            InstitutionId = null;
            TenantScopeContext.Set(null, isGlobalAdmin: true);
            return;
        }

        var userIdClaim = httpContext.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        int userId = 0;
        if (userIdClaim != null)
            int.TryParse(userIdClaim.Value, out userId);

        // admin.cross_institution_read capability'si global scope'ta varsa bypass
        IsGlobalAdmin = userId > 0 &&
                        resolver.Allows(userId, "admin.cross_institution_read", ctx: null);

        var instClaim = httpContext.User?.Claims
            .FirstOrDefault(c => c.Type == "InstitutionId");

        InstitutionId = instClaim != null && int.TryParse(instClaim.Value, out int iid)
            ? iid
            : 1; // anonim kullanıcı → public kurum (domain="public")

        TenantScopeContext.Set(InstitutionId, IsGlobalAdmin);
    }
}
