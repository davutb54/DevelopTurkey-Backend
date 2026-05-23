using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IWorkflowRunDal : IEntityRepository<WorkflowRun>
{
    /// <summary>Idempotency: aynı (eventId, definitionId, versionId) üçlüsü daha önce işlendi mi?</summary>
    WorkflowRun? FindExisting(Guid eventId, int definitionId, int versionId);

    List<WorkflowRun> GetByDefinition(int definitionId, int page = 1, int pageSize = 20);
    List<WorkflowRun> GetByInstitution(int institutionId, byte? status = null, int page = 1, int pageSize = 20);
    int CountByInstitution(int institutionId, byte? status = null);
}
