using Core.Entities;

namespace Entities.Concrete;

public class WorkflowTrigger : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string CodeName { get; set; }
    public string Description { get; set; }
    public string TargetEntity { get; set; }
    public bool IsActive { get; set; }
}
