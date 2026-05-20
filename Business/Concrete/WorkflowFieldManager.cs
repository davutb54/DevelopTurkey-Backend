using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Concrete;

public class WorkflowFieldManager : IWorkflowFieldService
{
    private readonly IWorkflowFieldDal _workflowFieldDal;

    public WorkflowFieldManager(IWorkflowFieldDal workflowFieldDal)
    {
        _workflowFieldDal = workflowFieldDal;
    }

    public Task<IDataResult<List<WorkflowField>>> GetAllActiveAsync()
    {
        var fields = _workflowFieldDal.GetAll(f => f.IsActive);
        return Task.FromResult<IDataResult<List<WorkflowField>>>(
            new SuccessDataResult<List<WorkflowField>>(fields));
    }
}
