namespace Entities.DTOs;

public class ReportAddDto
{
    public int ReporterUserId { get; set; }
    public string TargetType { get; set; }
    public int TargetId { get; set; }
    public string Reason { get; set; }
}