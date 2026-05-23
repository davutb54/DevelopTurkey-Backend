namespace Core.Utilities.Authorization;

public interface ICapabilityPolicy
{
    bool Allows(string capabilityCode, CapabilityRequestContext? ctx = null);
    void Require(string capabilityCode, CapabilityRequestContext? ctx = null);
}
