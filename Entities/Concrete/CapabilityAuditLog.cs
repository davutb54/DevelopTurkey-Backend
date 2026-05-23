using Core.Entities;

namespace Entities.Concrete;

public class CapabilityAuditLog : IEntity
{
    public int Id { get; set; }
    public int ActorUserId { get; set; }
    public int TargetUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
