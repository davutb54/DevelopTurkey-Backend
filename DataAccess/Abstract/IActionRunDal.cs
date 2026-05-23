using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IActionRunDal : IEntityRepository<ActionRun>
{
    List<ActionRun> GetByNodeRun(Guid nodeRunId);
}
