using Core.Entities;

namespace Entities.Concrete;

public class WorkflowField : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FieldPath { get; set; }
    public string DataType { get; set; }
    public bool IsActive { get; set; }
}
