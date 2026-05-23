using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(TemplateId), nameof(Version), IsUnique = true, Name = "UX_TemplateVersion_TemplateId_Version")]
public class TemplateVersion : IEntity
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPublished { get; set; } = false;

    [ForeignKey(nameof(TemplateId))]
    public CapabilityTemplate Template { get; set; } = null!;
}
