using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Concrete;

public class WorkflowTriggerManager : IWorkflowTriggerService
{
    private readonly IWorkflowTriggerDal _workflowTriggerDal;

    public WorkflowTriggerManager(IWorkflowTriggerDal workflowTriggerDal)
    {
        _workflowTriggerDal = workflowTriggerDal;
    }

    public Task<IDataResult<List<WorkflowTrigger>>> GetAllActiveAsync()
    {
        var triggers = _workflowTriggerDal.GetAll(t => t.IsActive);
        return Task.FromResult<IDataResult<List<WorkflowTrigger>>>(
            new SuccessDataResult<List<WorkflowTrigger>>(triggers));
    }
}
