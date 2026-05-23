namespace Entities.DTOs.Capability;

public class UserCapabilityDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CapabilityId { get; set; }
    public string CapabilityCode { get; set; } = string.Empty;
    public string CapabilityDescription { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? InstitutionId { get; set; }
    public string? ScopeJson { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? Reason { get; set; }
    public int Status { get; set; }
}
