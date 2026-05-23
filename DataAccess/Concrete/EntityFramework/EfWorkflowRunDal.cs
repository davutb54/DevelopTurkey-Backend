using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfWorkflowRunDal
    : EfEntityRepositoryBase<WorkflowRun, DevelopTurkeyContext>, IWorkflowRunDal
{
    public WorkflowRun? FindExisting(Guid eventId, int definitionId, int versionId)
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowRuns
            .FirstOrDefault(r =>
                r.EventId == eventId &&
                r.DefinitionId == definitionId &&
                r.VersionId == versionId);
    }

    public List<WorkflowRun> GetByDefinition(int definitionId, int page = 1, int pageSize = 20)
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowRuns
            .Where(r => r.DefinitionId == definitionId)
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public List<WorkflowRun> GetByInstitution(int institutionId, byte? status = null, int page = 1, int pageSize = 20)
    {
        using var context = new DevelopTurkeyContext();
        var q = context.WorkflowRuns.Where(r => r.InstitutionId == institutionId);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        return q.OrderByDescending(r => r.StartedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public int CountByInstitution(int institutionId, byte? status = null)
    {
        using var context = new DevelopTurkeyContext();
        var q = context.WorkflowRuns.Where(r => r.InstitutionId == institutionId);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        return q.Count();
    }
}
