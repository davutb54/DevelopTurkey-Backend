using System.Collections.Concurrent;

namespace Core.CrossCuttingConcerns.Monitoring;

public class SystemMonitorManager : ISystemMonitor
{
    private long _totalRequests = 0;
    private long _totalErrors = 0;
    private double _averageResponseTimeMs = 0;
    
    private readonly ConcurrentDictionary<string, DateTime> _activeUsers = new();
    private readonly ConcurrentQueue<TrafficDataPoint> _trafficHistory = new();
    
    private DateTime _lastDataPointTime = DateTime.UtcNow;

    public long TotalRequests => Interlocked.Read(ref _totalRequests);
    public long TotalErrors => Interlocked.Read(ref _totalErrors);
    public double AverageResponseTimeMs => _averageResponseTimeMs;

    public void RecordRequest(double responseTimeMs, bool isError, string clientIdentifier)
    {
        Interlocked.Increment(ref _totalRequests);
        if (isError) Interlocked.Increment(ref _totalErrors);

        // Moving Average
        double currentAvg = _averageResponseTimeMs;
        if (currentAvg == 0) _averageResponseTimeMs = responseTimeMs;
        else _averageResponseTimeMs = (currentAvg * 0.95) + (responseTimeMs * 0.05);

        // Update active user
        if (!string.IsNullOrEmpty(clientIdentifier))
        {
            _activeUsers[clientIdentifier] = DateTime.UtcNow;
        }

        // Keep a data point every ~3 seconds
        var now = DateTime.UtcNow;
        if ((now - _lastDataPointTime).TotalSeconds >= 3)
        {
            _lastDataPointTime = now;
            _trafficHistory.Enqueue(new TrafficDataPoint
            {
                Timestamp = now,
                ResponseTime = _averageResponseTimeMs,
                TotalRequests = TotalRequests
            });

            // Keep only the last 20 points (~1 minute)
            while (_trafficHistory.Count > 20)
            {
                _trafficHistory.TryDequeue(out _);
            }
        }
    }

    public int GetActiveUserCount(int minutesThreshold = 5)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-minutesThreshold);
        
        // Optional Cleanup
        var expiredKeys = _activeUsers.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _activeUsers.TryRemove(key, out _);
        }

        return _activeUsers.Count;
    }

    public List<TrafficDataPoint> GetTrafficHistory()
    {
        return _trafficHistory.ToList();
    }
}
