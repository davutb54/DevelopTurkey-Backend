using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IWorkflowVersionDal : IEntityRepository<WorkflowVersion>
{
    WorkflowVersion? GetLatestActive(int definitionId);
    List<WorkflowVersion> GetByDefinition(int definitionId);
}
