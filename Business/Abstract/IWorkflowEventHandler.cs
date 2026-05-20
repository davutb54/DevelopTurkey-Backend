using Business.Models;

namespace Business.Abstract;

public interface IWorkflowEventHandler
{
    Task HandleAsync(string eventName, RuleContext context);
}
