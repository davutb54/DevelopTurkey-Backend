using System;
using System.Collections.Generic;

namespace Entities.DTOs;

public class DashboardAnalyticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int BannedUsers { get; set; }
    
    public List<InstitutionProblemCountDto> ProblemsByInstitution { get; set; }
    public List<DailyUserRegistrationDto> UserRegistrationsLast30Days { get; set; }
}

public class InstitutionProblemCountDto
{
    public string InstitutionName { get; set; }
    public int Count { get; set; }
}

public class DailyUserRegistrationDto
{
    public string Date { get; set; }
    public int Count { get; set; }
}
