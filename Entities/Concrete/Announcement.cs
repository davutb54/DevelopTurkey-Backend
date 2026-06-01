using Core.Entities;

namespace Entities.Concrete;

public class Announcement : IEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetGroup { get; set; } = "all"; // all | registered | institution
    public int? InstitutionId { get; set; }
    public string? Link { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
