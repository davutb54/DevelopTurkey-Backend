using Core.Entities;

namespace Entities.DTOs;

public class EmailTemplateDto
{
    public int Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvailablePlaceholders { get; set; }
    public bool IsActive { get; set; }
}
