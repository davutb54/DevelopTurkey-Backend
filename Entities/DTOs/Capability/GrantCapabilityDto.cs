namespace Entities.DTOs.Capability;

public class GrantCapabilityDto
{
    public string CapabilityCode { get; set; } = string.Empty;
    public int? InstitutionId { get; set; }
    public string? ScopeJson { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
}
