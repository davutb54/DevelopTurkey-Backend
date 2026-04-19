using Core.Entities;

namespace Entities.Concrete;

public class UserWarning : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int IssuedByAdminId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime IssuedAt { get; set; } = DateTime.Now;
}
