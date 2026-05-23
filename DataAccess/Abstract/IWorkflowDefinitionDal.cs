using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IWorkflowDefinitionDal : IEntityRepository<WorkflowDefinition>
{
    List<WorkflowDefinition> GetActiveByTrigger(string triggerEvent, int institutionId);
}
