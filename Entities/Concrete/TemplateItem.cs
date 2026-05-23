using Core.Entities;

namespace Entities.Concrete;

public class TemplateItem : IEntity
{
    public int Id { get; set; }
    public int TemplateVersionId { get; set; }
    public int CapabilityId { get; set; }
    public string? ScopeJson { get; set; }
}
