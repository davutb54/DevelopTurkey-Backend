using Business.Abstract;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Concrete;

public class SecurityEventManager : ISecurityEventService
{
    private readonly ISecurityEventDal _dal;
    private readonly IMemoryCache _cache;

    // In-memory sliding window for enumeration detection: IP → (count, window-start)
    private static readonly object _enumLock = new();

    public SecurityEventManager(ISecurityEventDal dal, IMemoryCache cache)
    {
        _dal = dal;
        _cache = cache;
    }

    public void LogEvent(
        string eventType,
        string severity,
        string ipAddress,
        string? path,
        string? detail,
        int? userId = null,
        int? institutionId = null)
    {
        try
        {
            _dal.Add(new SecurityEvent
            {
                EventType    = eventType,
                Severity     = severity,
                IpAddress    = ipAddress,
                Path         = path,
                Detail       = detail,
                UserId       = userId,
                InstitutionId = institutionId,
                CreatedAt    = DateTime.UtcNow,
            });
        }
        catch
        {
            // Fire-and-forget: never crash the caller on audit write failure
        }
    }

    public (List<SecurityEvent> Items, int TotalCount) GetPaged(SecurityEventFilterDto filter)
        => _dal.GetPaged(filter);

    /// <summary>
    /// Track 404 hits per IP in a 5-minute sliding window. Returns true when the
    /// enumeration threshold is first crossed so the caller can log one event.
    /// </summary>
    public bool TrackNotFound(string ipAddress, int threshold = 30)
    {
        var key = $"sec_enum_{ipAddress}";
        lock (_enumLock)
        {
            var entry = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return new EnumCounter();
            })!;

            entry.Count++;
            // Fire exactly once when threshold is crossed
            return entry.Count == threshold;
        }
    }

    private sealed class EnumCounter { public int Count { get; set; } }
}
