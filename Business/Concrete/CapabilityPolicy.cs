using Core.Utilities.Authorization;
using Core.Utilities.Context;

namespace Business.Concrete;

public sealed class CapabilityPolicy : ICapabilityPolicy
{
    private readonly ICapabilityResolver _resolver;
    private readonly IClientContext _clientContext;

    public CapabilityPolicy(ICapabilityResolver resolver, IClientContext clientContext)
    {
        _resolver = resolver;
        _clientContext = clientContext;
    }

    public bool Allows(string capabilityCode, CapabilityRequestContext? ctx = null)
    {
        var userId = _clientContext.GetUserId();
        if (!userId.HasValue) return false;
        return _resolver.Allows(userId.Value, capabilityCode, ctx);
    }

    public void Require(string capabilityCode, CapabilityRequestContext? ctx = null)
    {
        if (!Allows(capabilityCode, ctx))
            throw new CapabilityDeniedException(capabilityCode);
    }
}
