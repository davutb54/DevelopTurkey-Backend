using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IWorkflowDeadLetterDal : IEntityRepository<WorkflowDeadLetter>
{
    List<WorkflowDeadLetter> GetPending(int page = 1, int pageSize = 50);
    int CountPending();
}
