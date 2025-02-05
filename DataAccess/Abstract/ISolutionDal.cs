using System.Linq.Expressions;
using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface ISolutionDal : IEntityRepository<Solution>
{
	List<SolutionDetailDto> GetSolutions(Expression<Func<SolutionDetailDto, bool>>? filter = null);
}