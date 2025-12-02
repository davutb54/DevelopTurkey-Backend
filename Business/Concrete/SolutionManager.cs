using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class SolutionManager : ISolutionService
{
	private readonly ISolutionDal _solutionDal = new EfSolutionDal();

	public IDataResult<Solution?> GetById(int id)
	{
		return new SuccessDataResult<Solution?>(_solutionDal.Get(solution => solution.Id == id));
	}

	public IDataResult<List<SolutionDetailDto>> GetAll()
	{
        return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions());
    }

	public IDataResult<List<SolutionDetailDto>> GetByProblem(int problemId)
	{
		return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions(solution => solution.ProblemId == problemId));
	}

	public IDataResult<List<SolutionDetailDto>> GetBySender(int senderId)
	{
		return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions(solution => solution.SenderId == senderId));
	}

	public IDataResult<List<SolutionDetailDto>> GetIsHighlighted()
	{
		return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions(solution => solution.IsHighlighted));
	}

	public IResult Add(Solution solution)
	{
		solution.SendDate = DateTime.Now;
		_solutionDal.Add(solution);
		return new SuccessResult(Messages.SolutionAdded);
	}

	public IResult Update(Solution solution)
	{
		_solutionDal.Update(solution);
		return new SuccessResult(Messages.SolutionUpdated);
	}

	public IResult Delete(int id)
	{
		_solutionDal.Delete(_solutionDal.Get(solution => solution.Id == id));
		return new SuccessResult(Messages.SolutionDeleted);
	}

    public int GetTotalCount()
    {
        return _solutionDal.Count();
    }
}