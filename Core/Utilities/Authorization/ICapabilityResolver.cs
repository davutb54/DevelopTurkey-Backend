namespace Core.Utilities.Authorization;

public interface ICapabilityResolver
{
    bool Allows(int userId, string capabilityCode, CapabilityRequestContext? ctx = null);
    IReadOnlyCollection<string> GetEffectiveCodes(int userId, int? institutionId = null);
    CapabilityResolutionResult Resolve(int userId, string capabilityCode, CapabilityRequestContext? ctx = null);
}

public sealed record CapabilityRequestContext(
    int? InstitutionId = null,
    string? Entity = null,
    int? EntityId = null,
    IReadOnlyCollection<string>? Fields = null);

public sealed record CapabilityResolutionResult(
    bool Allowed,
    string CapabilityCode,
    int? MatchedUserCapabilityId,
    string Reason);
