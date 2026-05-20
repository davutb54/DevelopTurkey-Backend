using Core.Entities;

namespace Entities.Concrete;

public class EmailTemplate : IEntity
{
    public int Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty; // e.g. "EmailVerification", "PasswordReset"
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; } // e.g. "{Name}, {Code}"
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
