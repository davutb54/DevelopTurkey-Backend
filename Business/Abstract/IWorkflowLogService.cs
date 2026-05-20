using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface IWorkflowLogService
{
    IResult Add(WorkflowLog log);
    IDataResult<List<WorkflowLog>> GetListByFilter(WorkflowLogFilterDto filter);
    IDataResult<int> CountByFilter(WorkflowLogFilterDto filter);
    IDataResult<WorkflowLog> GetById(int id);
}
