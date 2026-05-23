using System.Collections.Concurrent;
using Core.Utilities.Authorization;
using DataAccess.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

public sealed class CapabilitySnapshotService : ICapabilitySnapshot
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CapabilitySnapshotService> _logger;

    private readonly ConcurrentDictionary<int, List<UserCapabilitySnapshotEntry>> _byUser = new();
    private readonly ConcurrentDictionary<int, UserCapabilitySnapshotEntry> _byId = new();

    private int _count;
    private DateTime _loadedAt;

    public int Count => _count;
    public DateTime LoadedAt => _loadedAt;

    public CapabilitySnapshotService(IServiceScopeFactory scopeFactory, ILogger<CapabilitySnapshotService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var entries = await LoadAllAsync(ct);

        _byUser.Clear();
        _byId.Clear();
        _count = 0;

        foreach (var e in entries)
            InternalAdd(e);

        _loadedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "CapabilitySnapshot initialized: {Count} entries in {Ms}ms",
            _count, sw.ElapsedMilliseconds);
    }

    public IReadOnlyList<UserCapabilitySnapshotEntry> Get(int userId)
    {
        if (_byUser.TryGetValue(userId, out var list))
        {
            lock (list)
                return list.ToList();
        }
        return Array.Empty<UserCapabilitySnapshotEntry>();
    }

    public void AddOrReplace(UserCapabilitySnapshotEntry entry)
    {
        _byId[entry.Id] = entry;

        var list = _byUser.GetOrAdd(entry.UserId, _ => new List<UserCapabilitySnapshotEntry>());
        lock (list)
        {
            list.RemoveAll(e => e.Id == entry.Id);
            list.Add(entry);
        }

        Interlocked.Increment(ref _count);
    }

    public void Remove(int userCapabilityId)
    {
        if (!_byId.TryRemove(userCapabilityId, out var entry))
            return;

        if (_byUser.TryGetValue(entry.UserId, out var list))
        {
            lock (list)
                list.RemoveAll(e => e.Id == userCapabilityId);
        }

        Interlocked.Decrement(ref _count);
    }

    public async Task RefreshUserAsync(int userId)
    {
        var fresh = await LoadByUserAsync(userId);

        if (_byUser.TryGetValue(userId, out var oldList))
        {
            lock (oldList)
            {
                foreach (var old in oldList)
                    _byId.TryRemove(old.Id, out _);

                Interlocked.Add(ref _count, -oldList.Count);
                oldList.Clear();
            }
        }

        foreach (var e in fresh)
            InternalAdd(e);
    }

    private void InternalAdd(UserCapabilitySnapshotEntry entry)
    {
        _byId[entry.Id] = entry;
        var list = _byUser.GetOrAdd(entry.UserId, _ => new List<UserCapabilitySnapshotEntry>());
        lock (list)
            list.Add(entry);
        Interlocked.Increment(ref _count);
    }

    private async Task<List<UserCapabilitySnapshotEntry>> LoadAllAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var userCapDal = scope.ServiceProvider.GetRequiredService<IUserCapabilityDal>();
        var capDal = scope.ServiceProvider.GetRequiredService<ICapabilityDal>();

        return await Task.Run(() => BuildEntries(userCapDal, capDal, null), ct);
    }

    private async Task<List<UserCapabilitySnapshotEntry>> LoadByUserAsync(int userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var userCapDal = scope.ServiceProvider.GetRequiredService<IUserCapabilityDal>();
        var capDal = scope.ServiceProvider.GetRequiredService<ICapabilityDal>();

        return await Task.Run(() => BuildEntries(userCapDal, capDal, userId));
    }

    private static List<UserCapabilitySnapshotEntry> BuildEntries(
        IUserCapabilityDal userCapDal,
        ICapabilityDal capDal,
        int? userId)
    {
        var now = DateTime.UtcNow;

        var userCaps = userId.HasValue
            ? userCapDal.GetAll(uc =>
                uc.UserId == userId.Value &&
                uc.Status == 1 &&
                (uc.ExpiresAt == null || uc.ExpiresAt > now))
            : userCapDal.GetAll(uc =>
                uc.Status == 1 &&
                (uc.ExpiresAt == null || uc.ExpiresAt > now));

        if (userCaps.Count == 0)
            return new List<UserCapabilitySnapshotEntry>();

        var capIds = userCaps.Select(uc => uc.CapabilityId).Distinct().ToList();
        var caps = capDal.GetAll(c => capIds.Contains(c.Id) && c.IsActive)
            .ToDictionary(c => c.Id);

        return userCaps
            .Where(uc => caps.ContainsKey(uc.CapabilityId))
            .Select(uc => new UserCapabilitySnapshotEntry(
                uc.Id,
                uc.UserId,
                uc.CapabilityId,
                caps[uc.CapabilityId].Code,
                uc.InstitutionId,
                uc.ScopeJson,
                uc.ExpiresAt))
            .ToList();
    }
}
