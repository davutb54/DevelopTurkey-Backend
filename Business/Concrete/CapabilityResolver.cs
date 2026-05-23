using System.Text.Json;
using Core.Utilities.Authorization;

namespace Business.Concrete;

public sealed class CapabilityResolver : ICapabilityResolver
{
    private readonly ICapabilitySnapshot _snapshot;

    public CapabilityResolver(ICapabilitySnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public bool Allows(int userId, string capabilityCode, CapabilityRequestContext? ctx = null)
        => Resolve(userId, capabilityCode, ctx).Allowed;

    public IReadOnlyCollection<string> GetEffectiveCodes(int userId, int? institutionId = null)
    {
        var now = DateTime.UtcNow;
        var entries = _snapshot.Get(userId);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
        {
            if (e.ExpiresAt.HasValue && e.ExpiresAt.Value <= now)
                continue;

            if (institutionId.HasValue &&
                e.InstitutionId.HasValue &&
                e.InstitutionId.Value != institutionId.Value)
                continue;

            result.Add(e.CapabilityCode);
        }

        return result;
    }

    public CapabilityResolutionResult Resolve(int userId, string capabilityCode, CapabilityRequestContext? ctx = null)
    {
        var now = DateTime.UtcNow;
        var entries = _snapshot.Get(userId);

        foreach (var e in entries)
        {
            if (!e.CapabilityCode.Equals(capabilityCode, StringComparison.OrdinalIgnoreCase))
                continue;

            if (e.ExpiresAt.HasValue && e.ExpiresAt.Value <= now)
                return Deny(capabilityCode, "expired");

            if (!ScopeMatch(e, ctx))
                continue;

            return new CapabilityResolutionResult(true, capabilityCode, e.Id, "ok");
        }

        return entries.Any(e => e.CapabilityCode.Equals(capabilityCode, StringComparison.OrdinalIgnoreCase))
            ? Deny(capabilityCode, "scope_mismatch")
            : Deny(capabilityCode, "no_capability");
    }

    private static bool ScopeMatch(UserCapabilitySnapshotEntry entry, CapabilityRequestContext? ctx)
    {
        if (ctx == null)
            return !entry.InstitutionId.HasValue;

        if (entry.InstitutionId.HasValue)
        {
            if (!ctx.InstitutionId.HasValue || entry.InstitutionId.Value != ctx.InstitutionId.Value)
                return false;
        }

        if (string.IsNullOrWhiteSpace(entry.ScopeJson))
            return true;

        try
        {
            var scope = JsonSerializer.Deserialize<ScopeRule>(entry.ScopeJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (scope == null) return true;

            if (!string.IsNullOrEmpty(scope.Entity) && ctx.Entity != null &&
                !scope.Entity.Equals(ctx.Entity, StringComparison.OrdinalIgnoreCase))
                return false;

            if (scope.EntityId.HasValue && ctx.EntityId.HasValue &&
                scope.EntityId.Value != ctx.EntityId.Value)
                return false;

            if (scope.Fields != null && scope.Fields.Length > 0 &&
                ctx.Fields != null && ctx.Fields.Count > 0)
            {
                if (!scope.Fields.Any(f => ctx.Fields.Contains(f, StringComparer.OrdinalIgnoreCase)))
                    return false;
            }
        }
        catch
        {
            return true;
        }

        return true;
    }

    private static CapabilityResolutionResult Deny(string code, string reason)
        => new(false, code, null, reason);

    private sealed class ScopeRule
    {
        public string? Entity { get; set; }
        public int? EntityId { get; set; }
        public string[]? Fields { get; set; }
    }
}
