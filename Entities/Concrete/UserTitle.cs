using Core.Entities;

namespace Entities.Concrete;

public class UserTitle : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = "custom"; // official | expert | custom
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int InstitutionId { get; set; }
    public int AssignedByUserId { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
