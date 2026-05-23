using Core.Entities;

namespace Entities.Concrete;

public class TemplateVersion : IEntity
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPublished { get; set; } = false;
}
