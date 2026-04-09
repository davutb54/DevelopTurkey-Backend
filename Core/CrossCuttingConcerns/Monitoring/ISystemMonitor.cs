namespace Core.CrossCuttingConcerns.Monitoring;

public class TrafficDataPoint
{
    public DateTime Timestamp { get; set; }
    public double ResponseTime { get; set; }
    public long TotalRequests { get; set; }
}

public interface ISystemMonitor
{
    long TotalRequests { get; }
    long TotalErrors { get; }
    double AverageResponseTimeMs { get; }

    void RecordRequest(double responseTimeMs, bool isError, string clientIdentifier);
    
    int GetActiveUserCount(int minutesThreshold = 5);
    List<TrafficDataPoint> GetTrafficHistory();
}
