using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IRuleContextSnapshotDal : IEntityRepository<RuleContextSnapshot>
{
    RuleContextSnapshot? GetByRun(Guid runId);
}
