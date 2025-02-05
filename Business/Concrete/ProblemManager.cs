using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class ProblemManager : IProblemService
{
	private readonly IProblemDal _problemDal = new EfProblemDal();

	public IDataResult<ProblemDetailDto> GetById(int id)
	{
		return new SuccessDataResult<ProblemDetailDto>(_problemDal.GetProblemDetail(problem => problem.Id == id));
	}

	public IDataResult<List<Problem>> GetAll()
	{
		return new SuccessDataResult<List<Problem>>(_problemDal.GetAll());
	}

	public IDataResult<List<ProblemDetailDto>> GetByTopic(int topicId)
	{
		return new SuccessDataResult<List<ProblemDetailDto>>(_problemDal.GetProblemsDetails(problem => problem.TopicId == topicId));
	}

	public IDataResult<List<ProblemDetailDto>> GetBySender(int senderId)
	{
		return new SuccessDataResult<List<ProblemDetailDto>>(_problemDal.GetProblemsDetails(problem => problem.SenderId == senderId));
	}

	public IDataResult<List<ProblemDetailDto>> GetIsHighlighted()
	{
		return new SuccessDataResult<List<ProblemDetailDto>>(_problemDal.GetProblemsDetails(problem => problem.IsHighlighted));
	}

	public IResult Add(Problem problem)
	{
		problem.SendDate = DateTime.Now;
		_problemDal.Add(problem);
		return new SuccessResult(Messages.ProblemAdded);
	}

	public IResult Update(Problem problem)
	{
		_problemDal.Update(problem);
		return new SuccessResult(Messages.ProblemUpdated);
	}

	public IResult Delete(int id)
	{
		_problemDal.Delete(_problemDal.Get(problem => problem.Id == id));
		return new SuccessResult(Messages.ProblemDeleted);
	}
}