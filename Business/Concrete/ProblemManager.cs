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
	private readonly IProblemDal _problemDal;
	private readonly ILogDal _logDal;

    public ProblemManager(IProblemDal problemDal, ILogDal logDal)
    {
		_problemDal = problemDal;
		_logDal = logDal;
    }

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

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.ProblemAdded + $" - Başlık: {problem.Title}",
            Type = "Problem,Add,Info"
        });

        return new SuccessResult(Messages.ProblemAdded);
	}

	public IResult Update(Problem problem)
	{
		_problemDal.Update(problem);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.ProblemUpdated + $" - ID: {problem.Id}",
            Type = "Problem,Update,Info"
        });
        return new SuccessResult(Messages.ProblemUpdated);
	}

	public IResult Delete(int id)
	{
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");

        problem.IsDeleted = true;
        problem.DeleteDate = DateTime.Now;
        _problemDal.Update(problem);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.ProblemDeleted + $" - ID: {id}",
            Type = "Problem,Delete,Info"
        });

        return new SuccessResult(Messages.ProblemDeleted);
    }

    public IDataResult<List<ProblemDetailDto>> GetList(ProblemFilterDto filterDto)
    {
        return new SuccessDataResult<List<ProblemDetailDto>>(_problemDal.GetListByFilter(filterDto));
    }

    public IDataResult<List<ProblemDetailDto>> GetReportedProblems()
    {
        return new SuccessDataResult<List<ProblemDetailDto>>(
            _problemDal.GetProblemsDetails(p => p.IsReported == true)
        );
    }

    public int GetTotalCount()
    {
        return _problemDal.Count();
    }

    public int GetReportedCount()
    {
        return _problemDal.Count(p => p.IsReported == true);
    }
}