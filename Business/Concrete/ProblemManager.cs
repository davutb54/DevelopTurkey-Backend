using System.Linq;
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
    private readonly ISolutionDal _solutionDal;
    private readonly ICommentDal _commentDal;

    public ProblemManager(IProblemDal problemDal, ILogDal logDal, ISolutionDal solutionDal, ICommentDal commentDal)
    {
        _problemDal = problemDal;
        _logDal = logDal;
        _solutionDal = solutionDal;
        _commentDal = commentDal;
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

        var solutions = _solutionDal.GetAll(s => s.ProblemId == id && !s.IsDeleted);
        foreach (var sol in solutions)
        {
            sol.IsDeleted = true;
            sol.DeleteDate = DateTime.Now;
            _solutionDal.Update(sol);

            var comments = _commentDal.GetAll(c => c.SolutionId == sol.Id && !c.IsDeleted);
            foreach (var com in comments)
            {
                com.IsDeleted = true;
                com.DeleteDate = DateTime.Now;
                _commentDal.Update(com);

                var childComments = _commentDal.GetAll(cc => cc.ParentCommentId == com.Id && !cc.IsDeleted);
                foreach (var childCom in childComments)
                {
                    childCom.IsDeleted = true;
                    childCom.DeleteDate = DateTime.Now;
                    _commentDal.Update(childCom);
                }
            }
        }

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.ProblemDeleted + $" - ID: {id} (Alt Çözüm ve Yorumlarıyla Birlikte Silindi)",
            Type = "Problem,Delete,Info"
        });

        return new SuccessResult(Messages.ProblemDeleted);
    }

    public IDataResult<List<ProblemDetailDto>> GetList(ProblemFilterDto filterDto, int institutionId)
    {
        var problems = _problemDal.GetProblemsDetails(p =>
            (p.IsDeleted == false && p.InstitutionId == institutionId) &&
            (!filterDto.TopicId.HasValue || p.TopicId == filterDto.TopicId.Value) &&
            (!filterDto.CityCode.HasValue || p.CityCode == filterDto.CityCode.Value) &&
            (string.IsNullOrEmpty(filterDto.SearchText) || p.Title.Contains(filterDto.SearchText) || p.Description.Contains(filterDto.SearchText))
        );

        var sortedProblems = problems.OrderByDescending(p =>
            (p.ViewCount) +
            (p.SolutionCount * 5) +
            (p.IsResolvedByExpert ? 1000 : 0)
        ).ToList();

        var paginatedProblems = sortedProblems
            .Skip((filterDto.Page - 1) * filterDto.PageSize)
            .Take(filterDto.PageSize)
            .ToList();

        return new SuccessDataResult<List<ProblemDetailDto>>(paginatedProblems, "Sorunlar listelendi.");
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

    public IResult ReportProblem(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsReported = true;
        _problemDal.Update(problem);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Problem (ID: {problem.Id}) raporlandı.",
            Type = "Problem,Report,Info"
        });
        return new SuccessResult($"Problem (ID: {problem.Id}) raporlandı.");
    }

    public IResult UnReportProblem(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem != null)
        {
            problem.IsReported = false;
            _problemDal.Update(problem);
        }
        return new SuccessResult();
    }

    public IResult ToggleHighlight(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsHighlighted = !problem.IsHighlighted;
        _problemDal.Update(problem);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Problem (ID: {problem.Id}) {(problem.IsHighlighted ? "vurgulandı" : "vurgulama kaldırıldı")}.",
            Type = "Problem,Highlight,Info"
        });
        return new SuccessResult($"Problem (ID: {problem.Id}) {(problem.IsHighlighted ? "vurgulandı" : "vurgulama kaldırıldı")}.");
    }

    public IResult IncrementView(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem != null)
        {
            problem.ViewCount += 1;
            _problemDal.Update(problem);
        }
        return new SuccessResult();
    }

    public IResult ToggleResolved(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsResolved = !problem.IsResolved;
        _problemDal.Update(problem);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Problem (ID: {problem.Id}) {(problem.IsResolved ? "çözüldü" : "çözülmedi olarak işaretlendi")}.",
            Type = "Problem,Resolved,Info"
        });
        return new SuccessResult($"Problem (ID: {problem.Id}) {(problem.IsResolved ? "çözüldü" : "çözülmedi olarak işaretlendi")}.");
    }

    public IResult ResolveProblem(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsResolved = true;
        _problemDal.Update(problem);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Problem (ID: {problem.Id}) çözüldü.",
            Type = "Problem,Resolved,Info"
        });
        return new SuccessResult($"Problem (ID: {problem.Id}) çözüldü işaretlendi.");
    }
}