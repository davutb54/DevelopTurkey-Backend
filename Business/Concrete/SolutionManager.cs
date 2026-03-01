using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class SolutionManager : ISolutionService
{
    private readonly ISolutionDal _solutionDal;
    private readonly ILogDal _logDal;
    private readonly IProblemService _problemService;
    private readonly ICommentDal _commentDal;

    public SolutionManager(ISolutionDal solutionDal, ILogDal logDal, IProblemService problemService, ICommentDal commentDal)
    {
        _solutionDal = solutionDal;
        _logDal = logDal;
        _problemService = problemService;
        _commentDal = commentDal;
    }

    public IDataResult<Solution?> GetById(int id)
    {
        return new SuccessDataResult<Solution?>(_solutionDal.Get(solution => solution.Id == id));
    }

    public IDataResult<List<SolutionDetailDto>> GetAll(int institutionId)
    {
        return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions(s => s.InstitutionId == institutionId));
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

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.SolutionAdded + $" - ProblemID: {solution.ProblemId}",
            Type = "Solution,Add,Info"
        });
        return new SuccessResult(Messages.SolutionAdded);
    }

    public IResult Update(Solution solution)
    {
        _solutionDal.Update(solution);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.SolutionUpdated + $" - ID: {solution.Id}",
            Type = "Solution,Update,Info"
        });
        return new SuccessResult(Messages.SolutionUpdated);
    }

    public IResult Delete(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");

        solution.IsDeleted = true;
        solution.DeleteDate = DateTime.Now;
        _solutionDal.Update(solution);

        var comments = _commentDal.GetAll(c => c.SolutionId == id && !c.IsDeleted);
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

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.SolutionDeleted + $" - ID: {id} (Alt Yorumlarıyla Birlikte)",
            Type = "Solution,Delete,Info"
        });

        return new SuccessResult(Messages.SolutionDeleted);
    }

    public int GetTotalCount()
    {
        return _solutionDal.Count();
    }

    public IResult ReportSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.IsReported = true;
        _solutionDal.Update(solution);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Çözüm (ID: {solution.Id}) raporlandı.",
            Type = "Solution,Report,Info"
        });
        return new SuccessResult($"Çözüm (ID: {solution.Id}) raporlandı.");
    }

    public IResult UnReportSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution != null)
        {
            solution.IsReported = false;
            _solutionDal.Update(solution);
        }
        return new SuccessResult();
    }

    public IResult ToggleHighlight(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.IsHighlighted = !solution.IsHighlighted;
        _solutionDal.Update(solution);
        string action = solution.IsHighlighted ? "vurgulandı" : "vurgulama kaldırıldı";
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Çözüm (ID: {solution.Id}) {action}.",
            Type = "Solution,Highlight,Info"
        });
        return new SuccessResult($"Çözüm (ID: {solution.Id}) {action}.");
    }

    public IDataResult<List<SolutionDetailDto>> GetPendingExpertSolutions()
    {
        return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions(solution => solution.ExpertApprovalStatus == 0 && (solution.SenderIsExpert || solution.SenderIsOfficial)));
    }

    public IResult ApproveSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.ExpertApprovalStatus = 1;
        _solutionDal.Update(solution);

        _problemService.ResolveProblem(solution.ProblemId);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Çözüm (ID: {solution.Id}) admin tarafından onaylandı.",
            Type = "Solution,Approve,Info"
        });
        return new SuccessResult($"Çözüm (ID: {solution.Id}) admin tarafından onaylandı.");
    }

    public IResult RejectSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.ExpertApprovalStatus = 2;
        _solutionDal.Update(solution);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Çözüm (ID: {solution.Id}) admin tarafından reddedildi.",
            Type = "Solution,Reject,Info"
        });
        return new SuccessResult($"Çözüm (ID: {solution.Id}) admin tarafından reddedildi.");
    }
}