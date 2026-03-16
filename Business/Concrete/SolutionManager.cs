using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class SolutionManager : ISolutionService
{
    private readonly ISolutionDal _solutionDal;
    private readonly ILogService _logService;
    private readonly IProblemService _problemService;
    private readonly ICommentDal _commentDal;
    private readonly IClientContext _clientContext;

    public SolutionManager(ISolutionDal solutionDal, ILogService logService, IProblemService problemService, ICommentDal commentDal, IClientContext clientContext)
    {
        _solutionDal = solutionDal;
        _logService = logService;
        _problemService = problemService;
        _commentDal = commentDal;
        _clientContext = clientContext;
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
        solution.SenderId = _clientContext.GetUserId() ?? 0;
        solution.SendDate = DateTime.Now;
        _solutionDal.Add(solution);

        _logService.LogInfo("Content", "Add", $"Çözüm eklendi - ProblemID: {solution.ProblemId}");
        return new SuccessResult(Messages.SolutionAdded);
    }

    public IResult Update(Solution solution)
    {
        var currentUserId = _clientContext.GetUserId();
        var isAdmin = _clientContext.GetRoles().Contains("Admin");

        var existingSolution = _solutionDal.Get(s => s.Id == solution.Id);
        if (existingSolution == null) return new ErrorResult("Çözüm bulunamadı");

        if (!isAdmin && existingSolution.SenderId != currentUserId)
        {
            return new ErrorResult("Bu çözümü güncelleme yetkiniz yok.");
        }

        _solutionDal.Update(solution);
        _logService.LogInfo("Content", "Update", $"Çözüm güncellendi - ID: {solution.Id}");
        return new SuccessResult(Messages.SolutionUpdated);
    }

    public IResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var isAdmin = _clientContext.GetRoles().Contains("Admin");

        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");

        if (!isAdmin && solution.SenderId != currentUserId)
        {
            return new ErrorResult("Bu çözümü silme yetkiniz yok.");
        }

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

        _logService.LogWarning("Content", "Delete", $"Çözüm silindi - ID: {id} (Alt Yorumlarıyla Birlikte)");

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
        _logService.LogInfo("Moderation", "Report", $"Çözüm raporlandı - ID: {solution.Id}");
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
        _logService.LogInfo("Content", "Highlight", $"Çözüm {action} - ID: {solution.Id}");
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

        _logService.LogInfo("Moderation", "Approve", $"Çözüm onaylandı - ID: {solution.Id}");
        return new SuccessResult($"Çözüm (ID: {solution.Id}) admin tarafından onaylandı.");
    }

    public IResult RejectSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.ExpertApprovalStatus = 2;
        _solutionDal.Update(solution);
        _logService.LogInfo("Moderation", "Reject", $"Çözüm reddedildi - ID: {solution.Id}");
        return new SuccessResult($"Çözüm (ID: {solution.Id}) admin tarafından reddedildi.");
    }
    public IDataResult<List<SolutionDetailDto>> GetAllForAdmin()
    {
        return new SuccessDataResult<List<SolutionDetailDto>>(_solutionDal.GetSolutions());
    }
}