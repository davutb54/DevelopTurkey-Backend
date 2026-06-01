namespace Entities.DTOs;

public class AnnouncementDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetGroup { get; set; } = "all";
    public int? InstitutionId { get; set; }
    public string? Link { get; set; }
    public bool IsActive { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateAnnouncementDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetGroup { get; set; } = "all";
    public int? InstitutionId { get; set; }
    public string? Link { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
