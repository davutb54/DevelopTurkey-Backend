namespace Entities.DTOs.Capability;

public class CapabilityAuditFilterDto
{
    public int? ActorUserId { get; set; }
    public int? TargetUserId { get; set; }
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
