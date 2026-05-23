using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(UserId), nameof(Status), Name = "IX_UserCapability_UserId_Status")]
[Index(nameof(UserId), nameof(CapabilityId), Name = "IX_UserCapability_UserId_CapabilityId")]
[Index(nameof(InstitutionId), Name = "IX_UserCapability_InstitutionId")]
public class UserCapability : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CapabilityId { get; set; }
    public int? InstitutionId { get; set; }

    public string? ScopeJson { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int Status { get; set; } = 1;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(CapabilityId))]
    public Capability Capability { get; set; } = null!;

    [ForeignKey(nameof(InstitutionId))]
    public Institution? Institution { get; set; }
}
