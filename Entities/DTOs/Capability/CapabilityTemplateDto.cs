namespace Entities.DTOs.Capability;

public class CapabilityTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public TemplateVersionDto? LatestVersion { get; set; }
}

public class TemplateVersionDto
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public string? ChangeNote { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<TemplateItemDto> Items { get; set; } = new();
}

public class TemplateItemDto
{
    public int Id { get; set; }
    public int CapabilityId { get; set; }
    public string CapabilityCode { get; set; } = string.Empty;
    public string? CapabilityDescription { get; set; }
}

public class CreateTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> CapabilityCodes { get; set; } = new();
}

public class PublishTemplateVersionDto
{
    public List<string> CapabilityCodes { get; set; } = new();
    public string? ChangeNote { get; set; }
}

public class ApplyTemplateDto
{
    public int TemplateVersionId { get; set; }
    public List<int> UserIds { get; set; } = new();
    public int? InstitutionId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}
