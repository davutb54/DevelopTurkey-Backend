using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfNodeRunDal
    : EfEntityRepositoryBase<NodeRun, DevelopTurkeyContext>, INodeRunDal
{
    public List<NodeRun> GetByRun(Guid runId)
    {
        using var context = new DevelopTurkeyContext();
        return context.NodeRuns
            .Where(n => n.RunId == runId)
            .OrderBy(n => n.StartedAt)
            .ToList();
    }
}
