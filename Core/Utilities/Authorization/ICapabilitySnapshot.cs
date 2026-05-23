namespace Core.Utilities.Authorization;

public interface ICapabilitySnapshot
{
    Task InitializeAsync(CancellationToken ct = default);
    IReadOnlyList<UserCapabilitySnapshotEntry> Get(int userId);
    void AddOrReplace(UserCapabilitySnapshotEntry entry);
    void Remove(int userCapabilityId);
    Task RefreshUserAsync(int userId);
    int Count { get; }
    DateTime LoadedAt { get; }
}
