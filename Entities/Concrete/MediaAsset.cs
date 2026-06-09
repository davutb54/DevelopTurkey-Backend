using Core.Entities;

namespace Entities.Concrete;

public class MediaAsset : IEntity
{
    public int Id { get; set; }
    public string OwnerType { get; set; } = null!;  // "Problem", "Solution"
    public int OwnerId { get; set; }
    public string Kind { get; set; } = null!;        // "Image", "Video"
    public string FileName { get; set; } = null!;    // guid.mp4
    public string ContentType { get; set; } = null!; // video/mp4
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int InstitutionId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
