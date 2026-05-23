using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(TargetUserId), Name = "IX_CapabilityAuditLog_TargetUserId")]
[Index(nameof(ActorUserId), Name = "IX_CapabilityAuditLog_ActorUserId")]
[Index(nameof(Action), Name = "IX_CapabilityAuditLog_Action")]
[Index(nameof(CreatedAt), Name = "IX_CapabilityAuditLog_CreatedAt")]
public class CapabilityAuditLog : IEntity
{
    public int Id { get; set; }
    public int ActorUserId { get; set; }
    public int TargetUserId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
