using Core.Entities;

namespace Entities.Concrete;

public class Report : IEntity
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public string TargetType { get; set; }
    public int TargetId { get; set; }
    public string Reason { get; set; }
    public DateTime ReportDate { get; set; }
    public bool IsResolved { get; set; } = false;
}