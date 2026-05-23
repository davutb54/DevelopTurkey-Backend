using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface INodeRunDal : IEntityRepository<NodeRun>
{
    List<NodeRun> GetByRun(Guid runId);
}
