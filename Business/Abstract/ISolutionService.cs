using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ISolutionService
{
	IDataResult<Solution?> GetById(int id);
	IDataResult<List<SolutionDetailDto>> GetAll();
	IDataResult<List<SolutionDetailDto>> GetByProblem(int problemId);
	IDataResult<List<SolutionDetailDto>> GetBySender(int senderId);
	IDataResult<List<SolutionDetailDto>> GetIsHighlighted();
	IResult Add(Solution solution);
	IResult Update(Solution solution);
	IResult Delete(int id);
    int GetTotalCount();

}