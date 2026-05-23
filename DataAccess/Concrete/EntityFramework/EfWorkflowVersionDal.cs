using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfWorkflowVersionDal
    : EfEntityRepositoryBase<WorkflowVersion, DevelopTurkeyContext>, IWorkflowVersionDal
{
    public WorkflowVersion? GetLatestActive(int definitionId)
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowVersions
            .Where(v => v.DefinitionId == definitionId && v.Status == 1)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();
    }

    public List<WorkflowVersion> GetByDefinition(int definitionId)
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowVersions
            .Where(v => v.DefinitionId == definitionId)
            .OrderByDescending(v => v.Version)
            .ToList();
    }
}
