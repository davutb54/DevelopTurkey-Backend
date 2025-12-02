using System.Linq.Expressions;
using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface IProblemDal : IEntityRepository<Problem>
{
	List<ProblemDetailDto> GetProblemsDetails(Expression<Func<ProblemDetailDto, bool>>? filter = null);
	ProblemDetailDto GetProblemDetail(Expression<Func<ProblemDetailDto, bool>> filter);
    List<ProblemDetailDto> GetListByFilter(ProblemFilterDto filterDto);


}