using Core.Entities;

namespace Entities.Concrete;

public class WorkflowAction : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public string ParametersSchemaJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
