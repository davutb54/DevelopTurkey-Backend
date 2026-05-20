using Core.Entities;

namespace Entities.Concrete;

public class WorkflowAction : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ActionCode { get; set; }
    public string ParametersSchemaJson { get; set; }
    public bool IsActive { get; set; }
}
