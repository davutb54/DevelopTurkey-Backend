using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfActionRunDal
    : EfEntityRepositoryBase<ActionRun, DevelopTurkeyContext>, IActionRunDal
{
    public List<ActionRun> GetByNodeRun(Guid nodeRunId)
    {
        using var context = new DevelopTurkeyContext();
        return context.ActionRuns
            .Where(a => a.NodeRunId == nodeRunId)
            .OrderBy(a => a.StartedAt)
            .ToList();
    }
}
