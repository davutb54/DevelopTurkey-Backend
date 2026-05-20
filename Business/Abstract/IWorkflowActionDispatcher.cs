using Business.Models;
using Core.Utilities.Results;

namespace Business.Abstract;

public interface IWorkflowActionDispatcher
{
    Task<IDataResult<object?>> DispatchAsync(
        string actionCode,
        Dictionary<string, string> parameters,
        RuleContext context);
}
