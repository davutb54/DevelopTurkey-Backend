using Core.Entities;

namespace Entities.Concrete;

public class InstitutionFeatureValue : IEntity
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int FeatureDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty; // "true", "false", "60", "#ff0000" vb.
}
