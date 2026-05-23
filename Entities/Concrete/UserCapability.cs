using Core.Entities;

namespace Entities.Concrete;

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
    public string? Reason { get; set; }
    public int Status { get; set; } = 1;
}
