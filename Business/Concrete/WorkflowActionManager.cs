using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Concrete;

public class WorkflowActionManager : IWorkflowActionService
{
    private readonly IWorkflowActionDal _workflowActionDal;

    public WorkflowActionManager(IWorkflowActionDal workflowActionDal)
    {
        _workflowActionDal = workflowActionDal;
    }

    public Task<IDataResult<List<WorkflowAction>>> GetAllActiveAsync()
    {
        var actions = _workflowActionDal.GetAll(a => a.IsActive);
        return Task.FromResult<IDataResult<List<WorkflowAction>>>(
            new SuccessDataResult<List<WorkflowAction>>(actions));
    }
}
