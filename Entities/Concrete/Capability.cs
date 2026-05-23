using Core.Entities;

namespace Entities.Concrete;

public class Capability : IEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsSystem { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
