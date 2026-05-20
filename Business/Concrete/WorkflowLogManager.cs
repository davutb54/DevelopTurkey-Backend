using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class WorkflowLogManager : IWorkflowLogService
{
    private readonly IWorkflowLogDal _workflowLogDal;

    public WorkflowLogManager(IWorkflowLogDal workflowLogDal)
    {
        _workflowLogDal = workflowLogDal;
    }

    public IResult Add(WorkflowLog log)
    {
        _workflowLogDal.Add(log);
        return new SuccessResult();
    }

    public IDataResult<List<WorkflowLog>> GetListByFilter(WorkflowLogFilterDto filter)
    {
        var list = _workflowLogDal.GetListByFilter(filter);
        return new SuccessDataResult<List<WorkflowLog>>(list);
    }

    public IDataResult<int> CountByFilter(WorkflowLogFilterDto filter)
    {
        var count = _workflowLogDal.CountByFilter(filter);
        return new SuccessDataResult<int>(count);
    }

    public IDataResult<WorkflowLog> GetById(int id)
    {
        var log = _workflowLogDal.Get(l => l.Id == id);
        if (log == null) return new ErrorDataResult<WorkflowLog>(default, "Log bulunamadı.");
        return new SuccessDataResult<WorkflowLog>(log);
    }
}
