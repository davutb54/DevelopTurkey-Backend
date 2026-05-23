using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(TemplateVersionId), Name = "IX_TemplateItem_TemplateVersionId")]
public class TemplateItem : IEntity
{
    public int Id { get; set; }
    public int TemplateVersionId { get; set; }
    public int CapabilityId { get; set; }
    public string? ScopeJson { get; set; }

    [ForeignKey(nameof(TemplateVersionId))]
    public TemplateVersion TemplateVersion { get; set; } = null!;

    [ForeignKey(nameof(CapabilityId))]
    public Capability Capability { get; set; } = null!;
}
