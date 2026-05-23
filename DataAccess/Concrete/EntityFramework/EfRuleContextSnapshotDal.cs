using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfRuleContextSnapshotDal
    : EfEntityRepositoryBase<RuleContextSnapshot, DevelopTurkeyContext>, IRuleContextSnapshotDal
{
    public RuleContextSnapshot? GetByRun(Guid runId)
    {
        using var context = new DevelopTurkeyContext();
        return context.RuleContextSnapshots.FirstOrDefault(s => s.RunId == runId);
    }
}
