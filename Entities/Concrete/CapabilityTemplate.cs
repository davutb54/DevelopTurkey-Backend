using Core.Entities;

namespace Entities.Concrete;

public class CapabilityTemplate : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
