namespace Core.Utilities.Authorization;

public sealed record UserCapabilitySnapshotEntry(
    int Id,
    int UserId,
    int CapabilityId,
    string CapabilityCode,
    int? InstitutionId,
    string? ScopeJson,
    DateTime? ExpiresAt);
