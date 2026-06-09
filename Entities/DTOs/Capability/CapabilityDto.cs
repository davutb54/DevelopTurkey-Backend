namespace Entities.DTOs.Capability;

public class CapabilityDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? GroupKey { get; set; }
    public string PageScope { get; set; } = "Action";
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
