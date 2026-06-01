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

public class CommentManager : ICommentService
{
    private readonly ICommentDal _commentDal;
    private readonly ILogService _logService;
    private readonly IClientContext _clientContext;
    private readonly ISolutionDal _solutionDal;
    private readonly INotificationService _notificationService;
    private readonly IInstitutionFeatureService _featureService;
    private readonly IMentionService _mentionService;
    private readonly IWorkflowEventBus _eventBus;
    private readonly ICapabilityResolver _capabilityResolver;

    public CommentManager(ICommentDal commentDal, ILogService logService, IClientContext clientContext, ISolutionDal solutionDal, INotificationService notificationService, IInstitutionFeatureService featureService, IMentionService mentionService, IWorkflowEventBus eventBus, ICapabilityResolver capabilityResolver)
    {
        _commentDal = commentDal;
        _logService = logService;
        _clientContext = clientContext;
        _solutionDal = solutionDal;
        _notificationService = notificationService;
        _featureService = featureService;
        _mentionService = mentionService;
        _eventBus = eventBus;
        _capabilityResolver = capabilityResolver;
    }


    public IDataResult<Comment?> GetById(int id)
    {
        var comment = _commentDal.Get(c => c.Id == id);
        if (comment == null)
        {
            return new SuccessDataResult<Comment?>(null);
        }

        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentInstitutionId.HasValue)
        {
            var solution = _solutionDal.Get(s => s.Id == comment.SolutionId);
            if (solution == null || solution.InstitutionId != currentInstitutionId.Value)
            {
                return new SuccessDataResult<Comment?>(null);
            }
        }

        return new SuccessDataResult<Comment?>(comment);
    }

    public IDataResult<List<CommentDetailDto>> GetAll()
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (!currentInstitutionId.HasValue)
        {
            return new SuccessDataResult<List<CommentDetailDto>>(_commentDal.GetCommentDetails());
        }

        var allowedSolutionIds = _solutionDal
            .GetAll(s => s.InstitutionId == currentInstitutionId.Value && !s.IsDeleted)
            .Select(s => s.Id)
            .ToList();

        if (allowedSolutionIds.Count == 0)
        {
            return new SuccessDataResult<List<CommentDetailDto>>(new List<CommentDetailDto>());
        }

        var comments = _commentDal.GetCommentDetails(c => allowedSolutionIds.Contains(c.SolutionId));
        return new SuccessDataResult<List<CommentDetailDto>>(comments);
    }

    public IDataResult<List<CommentDetailDto>> GetByParentCommentId(int parentCommentId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentInstitutionId.HasValue)
        {
            var parent = _commentDal.Get(c => c.Id == parentCommentId);
            if (parent == null)
            {
                return new SuccessDataResult<List<CommentDetailDto>>(new List<CommentDetailDto>());
            }

            var parentSolution = _solutionDal.Get(s => s.Id == parent.SolutionId);
            if (parentSolution == null || parentSolution.InstitutionId != currentInstitutionId.Value)
            {
                return new SuccessDataResult<List<CommentDetailDto>>(new List<CommentDetailDto>());
            }
        }

        return new SuccessDataResult<List<CommentDetailDto>>(
            _commentDal.GetCommentDetails(comment => comment.ParentCommentId == parentCommentId));
    }

    public IDataResult<List<CommentDetailDto>> GetBySolution(int solutionId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentInstitutionId.HasValue)
        {
            var solution = _solutionDal.Get(s => s.Id == solutionId);
            if (solution == null || solution.InstitutionId != currentInstitutionId.Value)
            {
                return new SuccessDataResult<List<CommentDetailDto>>(new List<CommentDetailDto>());
            }
        }

        return new SuccessDataResult<List<CommentDetailDto>>(
            _commentDal.GetCommentDetails(comment => comment.SolutionId == solutionId));
    }

    public IResult Add(Comment comment)
    {
        var solution = _solutionDal.Get(s => s.Id == comment.SolutionId);
        if (solution == null) return new ErrorResult("Çözüm bulunamadı.");

        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentInstitutionId.HasValue && solution.InstitutionId != currentInstitutionId.Value)
        {
            return new ErrorResult("Bu kurumun içeriğine yorum ekleyemezsiniz.");
        }

        if (comment.ParentCommentId.HasValue && !_featureService.IsFeatureEnabled(solution.InstitutionId, "Social.EnableNestedComments"))
        {
            return new ErrorResult("Bu kurumda alt yorumlar (yanıtlar) devre dışıdır.");
        }

        comment.SenderId = _clientContext.GetUserId() ?? 0;
        comment.SendDate = DateTime.Now;
        _commentDal.Add(comment);

        _ = _eventBus.PublishAsync("comment.created", new RuleContext
        {
            SystemUserId = comment.SenderId,
            Metadata = new Dictionary<string, object?>
            {
                ["CommentId"] = comment.Id,
                ["SolutionId"] = comment.SolutionId,
                ["ParentCommentId"] = comment.ParentCommentId
            }
        });

        _logService.LogInfo("Content", "Add", $"Yorum eklendi - SolutionId: {comment.SolutionId}");

        // Etiketlemeleri işle
        _mentionService.ProcessMentions(comment.Text, comment.SenderId, solution.InstitutionId, $"/problem/{solution.ProblemId}", solution.Title);

        // Bildirim: Çözüm sahibine, kendi yorumu değilse bildirim gönder
        try
        {
            if (solution.SenderId != comment.SenderId)
            {
                _notificationService.Add(new Notification
                {
                    UserId = solution.SenderId,
                    Title = "Çözümünüze yorum yapıldı",
                    Message = "Paylaştığınız bir çözüme yeni bir yorum eklendi.",
                    Type = "CommentAdded",
                    ReferenceLink = $"/problem/{solution.ProblemId}"
                });
            }
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }

        return new SuccessResult(Messages.CommentAdded);
    }

    public IResult Update(CommentUpdateDto commentUpdateDto)
    {
        var currentUserId = _clientContext.GetUserId();
        var comment = _commentDal.Get(c => c.Id == commentUpdateDto.Id);
        if (comment == null) return new ErrorResult("Yorum Bulunamadı");

        var solution = _solutionDal.Get(s => s.Id == comment.SolutionId);
        var moderationCtx = new CapabilityRequestContext(
            InstitutionId: solution?.InstitutionId,
            Entity: "comment",
            EntityId: comment.Id);

        var isModerator = currentUserId.HasValue &&
                          _capabilityResolver.Allows(currentUserId.Value, "moderation.comment_moderate", moderationCtx);

        if (!isModerator && comment.SenderId != currentUserId)
        {
            return new ErrorResult("Bu yorumu güncelleme yetkiniz yok.");
        }
        comment.Text = commentUpdateDto.Text;

        _commentDal.Update(comment);

        _ = _eventBus.PublishAsync("comment.updated", new RuleContext
        {
            SystemUserId = comment.SenderId,
            Metadata = new Dictionary<string, object?>
            {
                ["CommentId"] = comment.Id,
                ["SolutionId"] = comment.SolutionId
            }
        });

        _logService.LogInfo("Content", "Update", $"Yorum güncellendi - CommentId: {comment.Id}");

        // Etiketlemeleri işle (güncellemede de bildirim gitsin)
        if (solution != null)
        {
            _mentionService.ProcessMentions(comment.Text, comment.SenderId, solution.InstitutionId, $"/problem/{solution.ProblemId}", solution.Title);
        }

        return new SuccessResult(Messages.CommentUpdated);
    }

    public IResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var comment = _commentDal.Get(comment => comment.Id == id);
        if (comment == null) return new ErrorResult("Yorum Bulunamadı");

        var solution = _solutionDal.Get(s => s.Id == comment.SolutionId);
        var moderationCtx = new CapabilityRequestContext(
            InstitutionId: solution?.InstitutionId,
            Entity: "comment",
            EntityId: comment.Id);

        var isModerator = currentUserId.HasValue &&
                          _capabilityResolver.Allows(currentUserId.Value, "moderation.comment_delete", moderationCtx);

        if (!isModerator && comment.SenderId != currentUserId)
        {
            return new ErrorResult("Bu yorumu silme yetkiniz yok.");
        }

        comment.IsDeleted = true;
        comment.DeleteDate = DateTime.Now;
        _commentDal.Update(comment);

        var childComments = _commentDal.GetAll(cc => cc.ParentCommentId == id && !cc.IsDeleted);
        foreach (var childCom in childComments)
        {
            childCom.IsDeleted = true;
            childCom.DeleteDate = DateTime.Now;
            _commentDal.Update(childCom);
        }

        _ = _eventBus.PublishAsync("comment.deleted", new RuleContext
        {
            SystemUserId = _clientContext.GetUserId() ?? 0,
            Metadata = new Dictionary<string, object?>
            {
                ["CommentId"] = id
            }
        });

        _logService.LogWarning("Content", "Delete", $"Yorum silindi - CommentId: {id} (Alt Yanıtlarıyla Birlikte)");

        return new SuccessResult(Messages.CommentDeleted);
    }
}