namespace Entities.DTOs;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalProblems { get; set; }
    public int TotalSolutions { get; set; }
    public int ReportedProblems { get; set; }
    public int BannedUsers { get; set; } 
}