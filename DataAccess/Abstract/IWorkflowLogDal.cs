using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface IWorkflowLogDal : IEntityRepository<WorkflowLog>
{
    List<WorkflowLog> GetListByFilter(WorkflowLogFilterDto filter);
    int CountByFilter(WorkflowLogFilterDto filter);
}
