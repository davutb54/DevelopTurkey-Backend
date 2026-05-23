namespace Entities.DTOs.Capability;

public class RevokeCapabilityDto
{
    public string CapabilityCode { get; set; } = string.Empty;
    public int? InstitutionId { get; set; }
    public string? Reason { get; set; }
}
