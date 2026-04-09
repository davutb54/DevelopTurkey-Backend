using System.Collections.Generic;
using Core.CrossCuttingConcerns.Monitoring;

namespace Entities.DTOs;

public class SystemHealthDto
{
    public long TotalRequests { get; set; }
    public long TotalErrors { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int ActiveUsers { get; set; }
    public double RamUsageMb { get; set; }
    public List<TrafficDataPoint> TrafficHistory { get; set; }
    public List<CityProblemDensityDto> TurkeyMapData { get; set; }
}

public class CityProblemDensityDto
{
    public int CityCode { get; set; }
    public int ProblemCount { get; set; }
    public int UserCount { get; set; }
}
