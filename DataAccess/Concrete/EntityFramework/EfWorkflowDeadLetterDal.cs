using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfWorkflowDeadLetterDal
    : EfEntityRepositoryBase<WorkflowDeadLetter, DevelopTurkeyContext>, IWorkflowDeadLetterDal
{
    public List<WorkflowDeadLetter> GetPending(int page = 1, int pageSize = 50)
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowDeadLetters
            .Where(d => !d.IsRequeued)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public int CountPending()
    {
        using var context = new DevelopTurkeyContext();
        return context.WorkflowDeadLetters.Count(d => !d.IsRequeued);
    }
}
