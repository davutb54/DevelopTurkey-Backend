using Business.Abstract;
using Business.Constants;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Authorization;
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
    private readonly INotificationService _notificationService;
    private readonly IProblemFollowService _problemFollowService;
    private readonly IInstitutionFeatureService _featureService;
    private readonly IMentionService _mentionService;
    private readonly IWorkflowEventBus _eventBus;
    private readonly ICapabilityResolver _capabilityResolver;
    private readonly IUserTitleDal _userTitleDal;

    public SolutionManager(ISolutionDal solutionDal, ILogService logService, IProblemService problemService, ICommentDal commentDal, IClientContext clientContext, INotificationService notificationService, IProblemFollowService problemFollowService, IInstitutionFeatureService featureService, IMentionService mentionService, IWorkflowEventBus eventBus, ICapabilityResolver capabilityResolver, IUserTitleDal userTitleDal)
    {
        _solutionDal = solutionDal;
        _logService = logService;
        _problemService = problemService;
        _commentDal = commentDal;
        _clientContext = clientContext;
        _notificationService = notificationService;
        _problemFollowService = problemFollowService;
        _featureService = featureService;
        _mentionService = mentionService;
        _eventBus = eventBus;
        _capabilityResolver = capabilityResolver;
        _userTitleDal = userTitleDal;
    }

    public IDataResult<Solution?> GetById(int id)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var solution = currentInstitutionId.HasValue
            ? _solutionDal.Get(s => s.Id == id && s.InstitutionId == currentInstitutionId.Value)
            : _solutionDal.Get(s => s.Id == id);

        return new SuccessDataResult<Solution?>(solution);
    }

    public IDataResult<List<SolutionDetailDto>> GetAll(int institutionId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentInstitutionId.HasValue) institutionId = currentInstitutionId.Value;
        var solutions = _solutionDal.GetSolutions(s => s.InstitutionId == institutionId);
        EnrichBadges(solutions);
        return new SuccessDataResult<List<SolutionDetailDto>>(solutions);
    }

    public IDataResult<List<SolutionDetailDto>> GetByProblem(int problemId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var solutions = currentInstitutionId.HasValue
            ? _solutionDal.GetSolutions(s => s.ProblemId == problemId && s.InstitutionId == currentInstitutionId.Value)
            : _solutionDal.GetSolutions(s => s.ProblemId == problemId);
        EnrichBadges(solutions);
        return new SuccessDataResult<List<SolutionDetailDto>>(solutions);
    }

    public IDataResult<List<SolutionDetailDto>> GetBySender(int senderId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var solutions = currentInstitutionId.HasValue
            ? _solutionDal.GetSolutions(s => s.SenderId == senderId && s.InstitutionId == currentInstitutionId.Value)
            : _solutionDal.GetSolutions(s => s.SenderId == senderId);
        EnrichBadges(solutions);
        return new SuccessDataResult<List<SolutionDetailDto>>(solutions);
    }

    public IDataResult<List<SolutionDetailDto>> GetIsHighlighted()
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var solutions = currentInstitutionId.HasValue
            ? _solutionDal.GetSolutions(s => s.IsHighlighted && s.InstitutionId == currentInstitutionId.Value)
            : _solutionDal.GetSolutions(s => s.IsHighlighted);
        EnrichBadges(solutions);
        return new SuccessDataResult<List<SolutionDetailDto>>(solutions);
    }

    public IResult Add(Solution solution)
    {
        solution.SenderId = _clientContext.GetUserId() ?? 0;
        var currentInstitutionId = _clientContext.GetInstitutionId();

        var problem = _problemService.GetById(solution.ProblemId);
        if (!problem.Success || problem.Data == null)
        {
            return new ErrorResult("Sorun bulunamadı.");
        }

        if (currentInstitutionId.HasValue && problem.Data.InstitutionId != currentInstitutionId.Value)
        {
            return new ErrorResult("Bu kurumun sorununa çözüm ekleyemezsiniz.");
        }

        if (problem.Data.IsClosed)
        {
            return new ErrorResult("Bu sorun kapatılmıştır, yeni çözüm eklenemiyor.");
        }

        // Kurum bilgisi, hedef problemin kurumundan türetilir.
        solution.InstitutionId = problem.Data.InstitutionId;
        solution.SendDate = DateTime.Now;

        if (!_featureService.IsFeatureEnabled(solution.InstitutionId, "Moderation.RequireExpertApproval", defaultValue: true))
        {
            solution.ExpertApprovalStatus = 1; // Otomatik Onay (özellik kapalıysa)
        }

        _solutionDal.Add(solution);

        _logService.LogInfo("Content", "Add", $"Çözüm eklendi - ProblemID: {solution.ProblemId}");

        // Bildirim: Problem sahibine, kendi çözümü değilse bildirim gönder
        try
        {
            if (problem.Success && problem.Data != null && problem.Data.SenderId != solution.SenderId)
            {
                // _notificationService.Add(new Notification
                // {
                //     UserId = problem.Data.SenderId,
                //     Title = "Sorununa yeni bir çözüm eklendi",
                //     Message = $"Birileri \"{problem.Data.Title}\" sorununa bir çözüm paylaştı.",
                //     Type = "SolutionAdded",
                //     ReferenceLink = $"/problem/{solution.ProblemId}"
                // });
            }

            // Bildirim: Problemi takip edenlere bildirim gönder
            var followerIds = _problemFollowService.GetFollowerIds(solution.ProblemId);
            foreach (var fId in followerIds)
            {
                if (fId == solution.SenderId) continue; // Kendine bildirim atma

                // Problem sahibine zaten üstte bildirim attık, tekrar atmayalım
                if (problem.Success && problem.Data != null && fId == problem.Data.SenderId) continue;

                // _notificationService.Add(new Notification
                // {
                //     UserId = fId,
                //     Title = "Takip ettiğiniz soruna yeni çözüm eklendi",
                //     Message = "Takip ettiğiniz bir soruna yeni bir çözüm eklendi.",
                //     Type = "FollowedProblemNewSolution",
                //     ReferenceLink = $"/problem/{solution.ProblemId}"
                // });
            }
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }

        // Etiketlemeleri işle
        _mentionService.ProcessMentions(solution.Description, solution.SenderId, solution.InstitutionId, $"/problem/{solution.ProblemId}", solution.Title);

        _ = _eventBus.PublishAsync("solution.created", new RuleContext
        {
            SystemUserId = solution.SenderId,
            SolutionId = solution.Id,
            ProblemId = solution.ProblemId,
            InstitutionId = solution.InstitutionId
        });

        return new SuccessResult(Messages.SolutionAdded);
    }

    public IResult Update(Solution solution)
    {
        var currentUserId = _clientContext.GetUserId();
        var existingSolution = _solutionDal.Get(s => s.Id == solution.Id);
        if (existingSolution == null) return new ErrorResult("Çözüm bulunamadı");

        var moderationCtx = new CapabilityRequestContext(
            InstitutionId: existingSolution.InstitutionId,
            Entity: "solution",
            EntityId: existingSolution.Id);

        var isModerator = currentUserId.HasValue &&
                          _capabilityResolver.Allows(currentUserId.Value, "moderation.solution_moderate", moderationCtx);

        if (!isModerator && existingSolution.SenderId != currentUserId)
        {
            return new ErrorResult("Bu çözümü güncelleme yetkiniz yok.");
        }

        // --- IDOR & Privilege Escalation Koruma Ağı ---
        solution.SenderId = existingSolution.SenderId;
        solution.SendDate = existingSolution.SendDate;
        solution.ProblemId = existingSolution.ProblemId;
        solution.InstitutionId = existingSolution.InstitutionId;

        if (!isModerator)
        {
            solution.IsReported = existingSolution.IsReported;
            solution.IsHighlighted = existingSolution.IsHighlighted;
            solution.ExpertApprovalStatus = existingSolution.ExpertApprovalStatus;
        }

        _solutionDal.Update(solution);
        _logService.LogInfo("Content", "Update", $"Çözüm güncellendi - ID: {solution.Id}");

        // Etiketlemeleri işle
        _mentionService.ProcessMentions(solution.Description, solution.SenderId, solution.InstitutionId, $"/problem/{solution.ProblemId}", solution.Title);

        _ = _eventBus.PublishAsync("solution.updated", new RuleContext
        {
            SystemUserId = (int)(currentUserId ?? 0),
            SolutionId = solution.Id,
            ProblemId = solution.ProblemId,
            InstitutionId = solution.InstitutionId
        });

        return new SuccessResult(Messages.SolutionUpdated);
    }

    public IResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");

        var moderationCtx = new CapabilityRequestContext(
            InstitutionId: solution.InstitutionId,
            Entity: "solution",
            EntityId: solution.Id);

        var isModerator = currentUserId.HasValue &&
                          _capabilityResolver.Allows(currentUserId.Value, "moderation.solution_delete", moderationCtx);

        if (!isModerator && solution.SenderId != currentUserId)
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

        if (isModerator && solution.SenderId != currentUserId)
        {
            _logService.LogWarning("AdminAction", "Delete", $"Çözüm GÖREVLİ tarafından silindi - ID: {id} (Alt Yorumlarıyla Birlikte)");
            try
            {
                // _notificationService.Add(new Notification
                // {
                //     UserId = solution.SenderId,
                //     Title = "Bir içeriğiniz kaldırıldı",
                //     Message = "Paylaştığınız bir çözüm platform kurallarına aykırı olduğu için kaldırıldı.",
                //     Type = "ContentRemoved",
                //     ReferenceLink = null
                // });
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }
        else
        {
            _logService.LogWarning("Content", "Delete", $"Çözüm kullanıcı tarafından silindi - ID: {id} (Alt Yorumlarıyla Birlikte)");
        }

        _ = _eventBus.PublishAsync("solution.deleted", new RuleContext
        {
            SystemUserId = (int)(currentUserId ?? 0),
            SolutionId = id,
            ProblemId = solution.ProblemId,
            TargetUserId = solution.SenderId,
            InstitutionId = solution.InstitutionId
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
        _logService.LogInfo("Moderation", "Report", $"Çözüm raporlandı - ID: {solution.Id}");

        _ = _eventBus.PublishAsync("solution.reported", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            SolutionId = id,
            ProblemId = solution.ProblemId,
            TargetUserId = solution.SenderId,
            InstitutionId = solution.InstitutionId
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

            _ = _eventBus.PublishAsync("solution.unreported", new RuleContext
            {
                SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
                SolutionId = id,
                ProblemId = solution.ProblemId,
                TargetUserId = solution.SenderId,
                InstitutionId = solution.InstitutionId
            });
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
        _logService.LogInfo("AdminAction", "Highlight", $"Çözüm {action} - ID: {solution.Id}");

        // Bildirim: sadece highlight açılıyorsa gönder
        if (solution.IsHighlighted)
        {
            try
            {
                // _notificationService.Add(new Notification
                // {
                //     UserId = solution.SenderId,
                //     Title = "Çözümünüz öne çıkarıldı ⭐",
                //     Message = "Paylaştığınız çözüm editörler tarafından öne çıkarıldı.",
                //     Type = "SolutionHighlighted",
                //     ReferenceLink = $"/problem/{solution.ProblemId}"
                // });
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }

        _ = _eventBus.PublishAsync("solution.highlight_toggled", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            SolutionId = id,
            ProblemId = solution.ProblemId,
            TargetUserId = solution.SenderId,
            NewValue = solution.IsHighlighted.ToString(),
            InstitutionId = solution.InstitutionId
        });

        return new SuccessResult($"Çözüm (ID: {solution.Id}) {action}.");
    }

    public IDataResult<List<SolutionDetailDto>> GetPendingExpertSolutions(int? institutionId = null)
    {
        var all = institutionId.HasValue
            ? _solutionDal.GetSolutions(s => s.ExpertApprovalStatus == 0 && s.InstitutionId == institutionId.Value)
            : _solutionDal.GetSolutions(s => s.ExpertApprovalStatus == 0);
        EnrichBadges(all);
        return new SuccessDataResult<List<SolutionDetailDto>>(
            all.Where(s => s.SenderIsExpert || s.SenderIsOfficial).ToList());
    }

    public IResult ApproveSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.ExpertApprovalStatus = 1;
        _solutionDal.Update(solution);

        _problemService.ResolveProblem(solution.ProblemId);

        _logService.LogInfo("AdminAction", "Approve", $"Çözüm onaylandı - ID: {solution.Id}");

        try
        {
            // _notificationService.Add(new Notification
            // {
            //     UserId = solution.SenderId,
            //     Title = "Çözümünüz onaylandı! 🎉",
            //     Message = "Paylaştığınız çözüm yetkili tarafından incelendi ve onaylandı.",
            //     Type = "SolutionApproved",
            //     ReferenceLink = $"/problem/{solution.ProblemId}"
            // });

            // Bildirim: Problemi takip edenlere "Çözüm yetkili tarafından onaylandı!" diye bildir
            var followerIds = _problemFollowService.GetFollowerIds(solution.ProblemId);
            foreach (var fId in followerIds)
            {
                if (fId == solution.SenderId) continue;

                // _notificationService.Add(new Notification
                // {
                //     UserId = fId,
                //     Title = "Takip ettiğiniz soruna onaylı çözüm!",
                //     Message = "Takip ettiğiniz bir sorundaki çözüm yetkililer tarafından onaylandı.",
                //     Type = "FollowedProblemSolutionApproved",
                //     ReferenceLink = $"/problem/{solution.ProblemId}"
                // });
            }
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }

        _ = _eventBus.PublishAsync("solution.approved", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            SolutionId = id,
            ProblemId = solution.ProblemId,
            TargetUserId = solution.SenderId,
            InstitutionId = solution.InstitutionId
        });

        return new SuccessResult($"Çözüm (ID: {solution.Id}) admin tarafından onaylandı.");
    }

    public IResult RejectSolution(int id)
    {
        var solution = _solutionDal.Get(s => s.Id == id);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı");
        solution.ExpertApprovalStatus = 2;
        _solutionDal.Update(solution);
        _logService.LogInfo("AdminAction", "Reject", $"Çözüm reddedildi - ID: {solution.Id}");

        try
        {
            // _notificationService.Add(new Notification
            // {
            //     UserId = solution.SenderId,
            //     Title = "Çözümünüz reddedildi",
            //     Message = "Paylaştığınız çözüm yetkili incelemesinden geçemedi.",
            //     Type = "SolutionRejected",
            //     ReferenceLink = $"/problem/{solution.ProblemId}"
            // });
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }

        _ = _eventBus.PublishAsync("solution.rejected", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            SolutionId = id,
            ProblemId = solution.ProblemId,
            TargetUserId = solution.SenderId,
            InstitutionId = solution.InstitutionId
        });

        return new SuccessResult($"Çözüm (ID: {solution.Id}) admin tarafından reddedildi.");
    }

    public IDataResult<List<SolutionDetailDto>> GetAllForAdmin(int? institutionId = null)
    {
        var solutions = institutionId.HasValue
            ? _solutionDal.GetSolutions(s => s.InstitutionId == institutionId.Value)
            : _solutionDal.GetSolutions();
        EnrichBadges(solutions);
        return new SuccessDataResult<List<SolutionDetailDto>>(solutions);
    }

    public int? GetSolutionInstitution(int solutionId)
        => _solutionDal.Get(s => s.Id == solutionId)?.InstitutionId;

    private void EnrichBadges(List<SolutionDetailDto> solutions)
    {
        if (solutions.Count == 0) return;

        var senderIds = solutions.Select(s => s.SenderId).Distinct().ToList();

        var allTitles = _userTitleDal.GetAll(t => senderIds.Contains(t.UserId) && t.IsVisible);
        var titleMap = allTitles.GroupBy(t => t.UserId).ToDictionary(
            g => g.Key,
            g => g.ToList());

        foreach (var s in solutions)
        {
            var titles = titleMap.GetValueOrDefault(s.SenderId, new List<UserTitle>());
            s.SenderIsExpert   = titles.Any(t => t.Kind == "expert");
            s.SenderIsOfficial = titles.Any(t => t.Kind == "official");
            s.SenderTitles = titles.Select(t => new UserTitleDto
            {
                Id = t.Id, UserId = t.UserId, Label = t.Label, Kind = t.Kind,
                Color = t.Color, Icon = t.Icon, IsVisible = t.IsVisible, AssignedAt = t.AssignedAt
            }).ToList();
        }
    }
}
