using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfWorkflowDefinitionDal
    : EfEntityRepositoryBase<WorkflowDefinition, DevelopTurkeyContext>, IWorkflowDefinitionDal
{
    public List<WorkflowDefinition> GetActiveByTrigger(string triggerEvent, int institutionId)
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowDefinitions
            .Where(d => d.IsActive
                        && d.TriggerEvent == triggerEvent
                        && d.InstitutionId == institutionId
                        && d.CurrentVersionId != null)
            .ToList();
    }
}
