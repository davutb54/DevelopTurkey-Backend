using System.Linq;
using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Concrete;

public class ProblemManager : IProblemService
{
    private readonly IProblemDal _problemDal;
    private readonly ILogService _logService;
    private readonly ISolutionDal _solutionDal;
    private readonly ICommentDal _commentDal;
    private readonly IProblemTopicDal _problemTopicDal;
    private readonly IClientContext _clientContext;
    private readonly IMemoryCache _cache;
    private readonly INotificationService _notificationService;
    private readonly IProblemFollowService _problemFollowService;
    private readonly ITopicFollowService _topicFollowService;
    private readonly IUserService _userService;
    private readonly ITopicFollowDal _topicFollowDal;
    private readonly IInstitutionFeatureService _featureService;
    private readonly IMentionService _mentionService;
    private readonly IWorkflowEventBus _eventBus;
    private readonly ICapabilityResolver _capabilityResolver;
    private readonly IUserTitleDal _userTitleDal;
    private readonly IOfficialResponseDal _officialResponseDal;
    private readonly IProblemUpvoteDal _problemUpvoteDal;
    private readonly IUserDal _userDal;

    public ProblemManager(IProblemDal problemDal, ILogService logService, ISolutionDal solutionDal, ICommentDal commentDal, IProblemTopicDal problemTopicDal, IClientContext clientContext, IMemoryCache cache, INotificationService notificationService, IProblemFollowService problemFollowService, ITopicFollowService topicFollowService, IUserService userService, ITopicFollowDal topicFollowDal, IInstitutionFeatureService featureService, IMentionService mentionService, IWorkflowEventBus eventBus, ICapabilityResolver capabilityResolver, IUserTitleDal userTitleDal, IOfficialResponseDal officialResponseDal, IProblemUpvoteDal problemUpvoteDal, IUserDal userDal)
    {
        _problemDal = problemDal;
        _logService = logService;
        _solutionDal = solutionDal;
        _commentDal = commentDal;
        _problemTopicDal = problemTopicDal;
        _clientContext = clientContext;
        _cache = cache;
        _notificationService = notificationService;
        _problemFollowService = problemFollowService;
        _topicFollowService = topicFollowService;
        _userService = userService;
        _topicFollowDal = topicFollowDal;
        _featureService = featureService;
        _mentionService = mentionService;
        _eventBus = eventBus;
        _capabilityResolver = capabilityResolver;
        _userTitleDal = userTitleDal;
        _officialResponseDal = officialResponseDal;
        _problemUpvoteDal = problemUpvoteDal;
        _userDal = userDal;
    }

    public IDataResult<ProblemDetailDto> GetById(int id)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var problem = currentInstitutionId.HasValue
            ? _problemDal.GetProblemDetail(p => p.Id == id && p.InstitutionId == currentInstitutionId.Value)
            : _problemDal.GetProblemDetail(p => p.Id == id);

        if (problem != null)
        {
            EnrichSingleBadge(problem);
            problem.OfficialResponses = LoadOfficialResponses(problem.Id);
        }

        return new SuccessDataResult<ProblemDetailDto>(problem);
    }

    public IDataResult<List<Problem>> GetAll()
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var problems = currentInstitutionId.HasValue
            ? _problemDal.GetAll(p => p.InstitutionId == currentInstitutionId.Value)
            : _problemDal.GetAll();

        return new SuccessDataResult<List<Problem>>(problems);
    }

    public IDataResult<List<ProblemDetailDto>> GetByTopic(int topicId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var problems = currentInstitutionId.HasValue
            ? _problemDal.GetProblemsDetails(p => p.InstitutionId == currentInstitutionId.Value)
            : _problemDal.GetProblemsDetails();
        var filteredProblems = problems.Where(p => p.Topics != null && p.Topics.Any(t => t.Id == topicId)).ToList();
        EnrichBadges(filteredProblems);
        return new SuccessDataResult<List<ProblemDetailDto>>(filteredProblems);
    }

    public IDataResult<List<ProblemDetailDto>> GetBySender(int senderId)
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var problems = currentInstitutionId.HasValue
            ? _problemDal.GetProblemsDetails(p => p.SenderId == senderId && p.InstitutionId == currentInstitutionId.Value)
            : _problemDal.GetProblemsDetails(p => p.SenderId == senderId);
        EnrichBadges(problems);
        return new SuccessDataResult<List<ProblemDetailDto>>(problems);
    }

    public IDataResult<List<ProblemDetailDto>> GetIsHighlighted()
    {
        var currentInstitutionId = _clientContext.GetInstitutionId();
        var problems = currentInstitutionId.HasValue
            ? _problemDal.GetProblemsDetails(p => p.IsHighlighted && p.InstitutionId == currentInstitutionId.Value)
            : _problemDal.GetProblemsDetails(p => p.IsHighlighted);
        EnrichBadges(problems);
        return new SuccessDataResult<List<ProblemDetailDto>>(problems);
    }

    public IResult Add(Problem problem, List<int> topicIds)
    {
        if (!_featureService.IsFeatureEnabled(problem.InstitutionId, "Content.EnableMapLocation", true))
        {
            problem.Latitude = 0;
            problem.Longitude = 0;
        }

        problem.SendDate = DateTime.Now;
        _problemDal.Add(problem);

        if (topicIds != null && topicIds.Count > 0)
        {
            foreach (var topicId in topicIds)
            {
                _problemTopicDal.Add(new ProblemTopic
                {
                    ProblemId = problem.Id,
                    TopicId = topicId
                });
            }
        }

        _logService.LogInfo("Content", "Add", $"Problem eklendi - Başlık: {problem.Title}");

        if (topicIds != null && topicIds.Count > 0) {
            var followerIds = _topicFollowService.GetFollowerIdsByTopicIds(topicIds);
            foreach (var fId in followerIds) {
                if (fId == problem.SenderId) continue; // Kendine atma

                // _notificationService.Add(new Notification {
                //     UserId = fId,
                //     Title = "Takip Ettiğiniz Kategoride Yeni Sorun",
                //     Message = $"\"{problem.Title}\" başlıklı yeni bir sorun paylaşıldı.",
                //     Type = "TopicNewProblem",
                //     ReferenceLink = $"/problem/{problem.Id}"
                // });
            }
        }

        // Etiketlemeleri işle
        _mentionService.ProcessMentions(problem.Description, problem.SenderId, problem.InstitutionId, $"/problem/{problem.Id}", problem.Title);

        _ = _eventBus.PublishAsync("problem.created", new RuleContext
        {
            SystemUserId = problem.SenderId,
            ProblemId = problem.Id,
            InstitutionId = problem.InstitutionId
        });

        return new SuccessResult(Messages.ProblemAdded);
    }

    public IResult Update(Problem problem, List<int> topicIds)
    {
        var currentUserId = _clientContext.GetUserId();
        var existingProblem = _problemDal.Get(p => p.Id == problem.Id);

        if (existingProblem == null)
        {
            return new ErrorResult("Kayıt bulunamadı");
        }

        var moderationCtx = new CapabilityRequestContext(
            InstitutionId: existingProblem.InstitutionId,
            Entity: "problem",
            EntityId: existingProblem.Id);

        var isModerator = currentUserId.HasValue &&
                          _capabilityResolver.Allows(currentUserId.Value, "moderation.problem_moderate", moderationCtx);

        // TODO: İleride Moderator rolü (Örn: IsOfficial) eklendiğinde, moderatörün
        // kendi kurumuna (InstitutionId) ait olmayan problemleri güncellemesi engellenmelidir.
        // Örn: var institutionId = _clientContext.GetInstitutionId();
        // if (!isAdmin && isOfficial && existingProblem.InstitutionId != institutionId) return new ErrorResult(Messages.AuthorizationDenied);

        if (!isModerator && existingProblem.SenderId != currentUserId)
        {
             return new ErrorResult("Bu sorunu güncelleme yetkiniz yok.");
        }

        // --- IDOR & Privilege Escalation Koruma Ağı ---
        // Kullanıcı yetkili dahi olsa (Admin veya kendi gönderisi) formdan gelebilecek
        // manipüle edilmiş metadataların/sahipliğin üzerine DB'den gelen orjinal hallerini eziyoruz.
        problem.SenderId = existingProblem.SenderId;
        problem.SendDate = existingProblem.SendDate;
        problem.InstitutionId = existingProblem.InstitutionId;
        problem.ViewCount = existingProblem.ViewCount;

        // Yalnızca moderatörlerin müdahale edebileceği alanlar; değilse DB değerini koru.
        if (!isModerator)
        {
            problem.IsReported = existingProblem.IsReported;
            problem.IsHighlighted = existingProblem.IsHighlighted;
            problem.IsResolved = existingProblem.IsResolved;
        }

        _problemDal.Update(problem);

        var existingTopics = _problemTopicDal.GetAll(pt => pt.ProblemId == problem.Id);
        foreach (var pt in existingTopics)
        {
            _problemTopicDal.Delete(pt);
        }

        if (topicIds != null && topicIds.Count > 0)
        {
            foreach (var topicId in topicIds)
            {
                _problemTopicDal.Add(new ProblemTopic
                {
                    ProblemId = problem.Id,
                    TopicId = topicId
                });
            }
        }

        _logService.LogInfo("Content", "Update", $"Problem güncellendi - ID: {problem.Id}");

        // Etiketlemeleri işle
        _mentionService.ProcessMentions(problem.Description, problem.SenderId, problem.InstitutionId, $"/problem/{problem.Id}", problem.Title);

        _ = _eventBus.PublishAsync("problem.updated", new RuleContext
        {
            SystemUserId = (int)(currentUserId ?? 0),
            ProblemId = problem.Id,
            InstitutionId = problem.InstitutionId
        });

        return new SuccessResult(Messages.ProblemUpdated);
    }

    public IResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");

        var moderationCtx = new CapabilityRequestContext(
            InstitutionId: problem.InstitutionId,
            Entity: "problem",
            EntityId: problem.Id);

        var isModerator = currentUserId.HasValue &&
                          _capabilityResolver.Allows(currentUserId.Value, "moderation.problem_delete", moderationCtx);

        if (!isModerator && problem.SenderId != currentUserId)
        {
             return new ErrorResult("Bu sorunu silme yetkiniz yok.");
        }

        problem.IsDeleted = true;
        problem.DeleteDate = DateTime.Now;
        _problemDal.Update(problem);

        var problemTopics = _problemTopicDal.GetAll(pt => pt.ProblemId == id);
        foreach (var pt in problemTopics)
        {
            _problemTopicDal.Delete(pt);
        }

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

        if (isModerator && problem.SenderId != currentUserId)
        {
            _logService.LogWarning("AdminAction", "Delete", $"Problem GÖREVLİ tarafından silindi - ID: {id} (Alt Çözüm ve Yorumlarıyla Birlikte)");
            try
            {
                // _notificationService.Add(new Notification
                // {
                //     UserId = problem.SenderId,
                //     Title = "Bir içeriğiniz kaldırıldı",
                //     Message = $"\"{problem.Title}\" başlıklı sorunuz platform kurallarına aykırı olduğu için kaldırıldı.",
                //     Type = "ContentRemoved",
                //     ReferenceLink = null
                // });
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }
        else
        {
            _logService.LogWarning("Content", "Delete", $"Problem kullanıcı tarafından silindi - ID: {id} (Alt Çözüm ve Yorumlarıyla Birlikte)");
        }

        _ = _eventBus.PublishAsync("problem.deleted", new RuleContext
        {
            SystemUserId = (int)(currentUserId ?? 0),
            ProblemId = id,
            TargetUserId = problem.SenderId,
            InstitutionId = problem.InstitutionId
        });

        return new SuccessResult(Messages.ProblemDeleted);
    }

    public IDataResult<List<ProblemDetailDto>> GetList(ProblemFilterDto filterDto, int institutionId)
    {
        var problems = _problemDal.GetProblemsDetails(p =>
            (p.IsDeleted == false) &&
            (p.IsHidden == false) &&
            (institutionId == 0 || p.InstitutionId == institutionId) &&
            (!filterDto.CityCode.HasValue || p.CityCode == filterDto.CityCode.Value) &&
            (!filterDto.CustomHierarchyId.HasValue || p.CustomHierarchyId == filterDto.CustomHierarchyId.Value) &&
            (string.IsNullOrEmpty(filterDto.SearchText) || p.Title.Contains(filterDto.SearchText) || p.Description.Contains(filterDto.SearchText))
        );

        if (filterDto.TopicId.HasValue && filterDto.TopicId.Value > 0)
        {
            problems = problems.Where(p => p.Topics != null && p.Topics.Any(t => t.Id == filterDto.TopicId.Value)).ToList();
        }

        var currentUserId = _clientContext.GetUserId();
        int? userCityCode = null;
        int? userInstitutionId = null;
        List<int> followedTopicIds = new List<int>();

        if (currentUserId > 0)
        {
            var user = _userService.GetById((int)currentUserId).Data;
            if (user != null)
            {
                userCityCode = user.CityCode;
                userInstitutionId = user.InstitutionId;
            }
            followedTopicIds = _topicFollowDal.GetAll(t => t.UserId == currentUserId).Select(t => t.TopicId).ToList();
        }

        var sortedProblems = problems.OrderByDescending(p => {
            // 1. Etkileşim Skoru
            double baseScore = (p.ViewCount * 1) +
                               (p.SolutionCount * 15) +
                               (p.UpvoteCount * 20) +
                               (p.FollowerCount * 10);
            if (baseScore == 0) baseScore = 1;

            // 2. Otorite Çarpanı
            double authority = 1.0;
            if (p.IsHighlighted) authority *= 3.0;
            if (p.IsResolvedByExpert) authority *= 2.0;
            if (p.SenderIsOfficial) authority *= 1.5;

            // 3. Kişiselleştirme Çarpanı
            double personalization = 1.0;
            if (currentUserId > 0) {
                if (userCityCode.HasValue && p.CityCode == userCityCode.Value) personalization *= 1.5;
                if (userInstitutionId.HasValue && p.InstitutionId == userInstitutionId.Value) personalization *= 2.0;
                if (p.Topics != null && p.Topics.Any(t => followedTopicIds.Contains(t.Id))) personalization *= 2.5;
            }

            // 4. Zaman Kaybı (Time Decay)
            double hoursSincePosted = (DateTime.Now - p.SendDate).TotalHours;
            if (hoursSincePosted < 0) hoursSincePosted = 0;
            double timeDecay = Math.Pow(hoursSincePosted + 2, 1.5);

            return (baseScore * authority * personalization) / timeDecay;
        }).ToList();

        var paginatedProblems = sortedProblems
            .Skip((filterDto.Page - 1) * filterDto.PageSize)
            .Take(filterDto.PageSize)
            .ToList();

        return new SuccessDataResult<List<ProblemDetailDto>>(paginatedProblems, "Sorunlar listelendi.");
    }

    public IDataResult<List<ProblemDetailDto>> GetReportedProblems(int? institutionId = null)
    {
        return new SuccessDataResult<List<ProblemDetailDto>>(
            institutionId.HasValue
                ? _problemDal.GetProblemsDetails(p => p.IsReported == true && p.InstitutionId == institutionId.Value)
                : _problemDal.GetProblemsDetails(p => p.IsReported == true)
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
        _logService.LogInfo("Moderation", "Report", $"Problem raporlandı - ID: {problem.Id}");

        _ = _eventBus.PublishAsync("problem.reported", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            ProblemId = id,
            TargetUserId = problem.SenderId,
            InstitutionId = problem.InstitutionId
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

            _ = _eventBus.PublishAsync("problem.unreported", new RuleContext
            {
                SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
                ProblemId = id,
                TargetUserId = problem.SenderId,
                InstitutionId = problem.InstitutionId
            });
        }
        return new SuccessResult();
    }

    public IResult ToggleHighlight(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsHighlighted = !problem.IsHighlighted;
        _problemDal.Update(problem);
        _logService.LogInfo("AdminAction", "Highlight", $"Problem {(problem.IsHighlighted ? "vurgulandı" : "vurgulama kaldırıldı")} - ID: {problem.Id}");

        if (problem.IsHighlighted)
        {
            try
            {
                var followerIds = _problemFollowService.GetFollowerIds(problem.Id);
                foreach (var fId in followerIds)
                {
                    // _notificationService.Add(new Notification
                    // {
                    //     UserId = fId,
                    //     Title = "Takip ettiğiniz sorun öne çıkarıldı!",
                    //     Message = $"\"{problem.Title}\" başlıklı sorun editörler tarafından öne çıkarıldı.",
                    //     Type = "FollowedProblemHighlighted",
                    //     ReferenceLink = $"/problem/{problem.Id}"
                    // });
                }
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }

        _ = _eventBus.PublishAsync("problem.highlight_toggled", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            ProblemId = id,
            NewValue = problem.IsHighlighted.ToString(),
            InstitutionId = problem.InstitutionId
        });

        return new SuccessResult($"Problem (ID: {problem.Id}) {(problem.IsHighlighted ? "vurgulandı" : "vurgulama kaldırıldı")}.");
    }

    public IResult IncrementView(int id, string ipAddress)
    {
        string cacheKey = $"View_Problem_{id}_{ipAddress}";

        if (_cache.TryGetValue(cacheKey, out _))
        {
            // Aynı IP 10 dakika içinde tekrar istek attı, sayacı artırma
            return new SuccessResult();
        }

        var problem = _problemDal.Get(p => p.Id == id);
        if (problem != null)
        {
            problem.ViewCount += 1;
            _problemDal.Update(problem);

            _ = _eventBus.PublishAsync("problem.view_incremented", new RuleContext
            {
                ProblemId = id,
                InstitutionId = problem.InstitutionId
            });
        }

        // 10 dakika boyunca bu IP'nin bu problemi tekrar count etmesini engelle
        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(10));
        return new SuccessResult();
    }

    public IResult ToggleResolved(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsResolved = !problem.IsResolved;
        _problemDal.Update(problem);
        _logService.LogInfo("AdminAction", "ToggleResolved", $"Problem {(problem.IsResolved ? "çözüldü" : "çözülmedi olarak işaretlendi")} - ID: {problem.Id}");

        _ = _eventBus.PublishAsync("problem.resolved_toggled", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            ProblemId = id,
            NewValue = problem.IsResolved.ToString(),
            InstitutionId = problem.InstitutionId
        });

        return new SuccessResult($"Problem (ID: {problem.Id}) {(problem.IsResolved ? "çözüldü" : "çözülmedi olarak işaretlendi")}.");
    }

    public IResult ResolveProblem(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsResolved = true;
        _problemDal.Update(problem);
        _logService.LogInfo("Content", "Resolve", $"Problem çözüldü - ID: {problem.Id}");

        _ = _eventBus.PublishAsync("problem.resolved", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            ProblemId = id,
            TargetUserId = problem.SenderId,
            InstitutionId = problem.InstitutionId
        });

        return new SuccessResult($"Problem (ID: {problem.Id}) çözüldü işaretlendi.");
    }

    public IDataResult<List<ProblemDetailDto>> GetAllForAdmin(int? institutionId = null)
    {
        var problems = institutionId.HasValue
            ? _problemDal.GetProblemsDetails(p => p.IsDeleted == false && p.InstitutionId == institutionId.Value)
            : _problemDal.GetProblemsDetails(p => p.IsDeleted == false);
        EnrichBadges(problems);
        return new SuccessDataResult<List<ProblemDetailDto>>(problems.OrderByDescending(p => p.SendDate).ToList());
    }

    public int? GetProblemInstitution(int problemId)
        => _problemDal.Get(p => p.Id == problemId)?.InstitutionId;

    public IResult RemoveTopicFromProblem(int problemId, int topicId)
    {
        var problemTopic = _problemTopicDal.Get(pt => pt.ProblemId == problemId && pt.TopicId == topicId);
        if (problemTopic != null)
        {
            _problemTopicDal.Delete(problemTopic);
            _logService.LogInfo("AdminAction", "RemoveTopic", $"Problemden kategori silindi - ProblemID: {problemId}, TopicID: {topicId}");

            try
            {
                var problem = _problemDal.Get(p => p.Id == problemId);
                if (problem != null)
                {
                    // _notificationService.Add(new Notification
                    // {
                    //     UserId = problem.SenderId,
                    //     Title = "Sorunuzdan bir kategori kaldırıldı",
                    //     Message = "Yöneticiler, paylaştığınız sorundan uygunsuz bir kategoriyi kaldırdı.",
                    //     Type = "ContentModified",
                    //     ReferenceLink = $"/problem/{problemId}"
                    // });
                }
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }

            _ = _eventBus.PublishAsync("problem.topic_removed", new RuleContext
            {
                SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
                ProblemId = problemId,
                Metadata = new Dictionary<string, object?> { ["TopicId"] = topicId }
            });

            return new SuccessResult("Kategori sorundan başarıyla kaldırıldı.");
        }
        return new ErrorResult("Bu sorunda böyle bir kategori bulunamadı.");
    }

    public IResult AssignToInstitution(int problemId, int institutionId)
    {
        var problem = _problemDal.Get(p => p.Id == problemId && !p.IsDeleted);
        if (problem is null)
            return new ErrorResult($"Problem bulunamadı (ID: {problemId}).");

        problem.InstitutionId = institutionId;
        _problemDal.Update(problem);
        _logService.LogInfo("WorkflowAction", "AssignToInstitution",
            $"Problem {problemId} kuruma atandı (InstitutionId: {institutionId}).");
        return new SuccessResult($"Problem (ID: {problemId}) kuruma atandı.");
    }

    public IResult CloseProblem(int id, string? reason)
    {
        var problem = _problemDal.Get(p => p.Id == id && !p.IsDeleted);
        if (problem is null) return new ErrorResult("Sorun bulunamadı.");
        problem.IsClosed = true;
        problem.ClosedAt = DateTime.Now;
        problem.ClosedByUserId = (int?)_clientContext.GetUserId();
        problem.CloseReason = reason;
        _problemDal.Update(problem);
        _logService.LogInfo("Moderation", "CloseProblem", $"Problem kapatıldı - ID: {id}, Sebep: {reason}");
        _ = _eventBus.PublishAsync("problem.closed", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            ProblemId = id,
            TargetUserId = problem.SenderId,
            NewValue = reason,
            InstitutionId = problem.InstitutionId
        });
        return new SuccessResult("Sorun kapatıldı.");
    }

    public IResult ReopenProblem(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id && !p.IsDeleted);
        if (problem is null) return new ErrorResult("Sorun bulunamadı.");
        problem.IsClosed = false;
        problem.ClosedAt = null;
        problem.ClosedByUserId = null;
        problem.CloseReason = null;
        _problemDal.Update(problem);
        _logService.LogInfo("Moderation", "ReopenProblem", $"Problem yeniden açıldı - ID: {id}");
        _ = _eventBus.PublishAsync("problem.reopened", new RuleContext
        {
            SystemUserId = (int)(_clientContext.GetUserId() ?? 0),
            ProblemId = id,
            TargetUserId = problem.SenderId,
            InstitutionId = problem.InstitutionId
        });
        return new SuccessResult("Sorun yeniden açıldı.");
    }

    public IResult ToggleHide(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id && !p.IsDeleted);
        if (problem is null) return new ErrorResult("Sorun bulunamadı.");
        problem.IsHidden = !problem.IsHidden;
        _problemDal.Update(problem);
        _logService.LogInfo("Moderation", "ToggleHide", $"Problem {(problem.IsHidden ? "gizlendi" : "gösterildi")} - ID: {id}");
        return new SuccessResult($"Sorun {(problem.IsHidden ? "gizlendi" : "görünür yapıldı")}.");
    }

    public IResult SetStatus(int problemId, string status, bool value)
    {
        var problem = _problemDal.Get(p => p.Id == problemId && !p.IsDeleted);
        if (problem is null)
            return new ErrorResult($"Problem bulunamadı (ID: {problemId}).");

        switch (status.ToLowerInvariant())
        {
            case "resolved":    problem.IsResolved    = value; break;
            case "highlighted": problem.IsHighlighted = value; break;
            case "reported":    problem.IsReported    = value; break;
            default:
                return new ErrorResult($"Geçersiz status değeri: '{status}'. Kabul edilenler: resolved, highlighted, reported.");
        }

        _problemDal.Update(problem);
        _logService.LogInfo("WorkflowAction", "SetStatus",
            $"Problem {problemId} durumu güncellendi: {status}={value}.");
        return new SuccessResult($"Problem (ID: {problemId}) {status} → {value}.");
    }

    private void EnrichSingleBadge(ProblemDetailDto problem)
    {
        var titles = _userTitleDal.GetAll(t => t.UserId == problem.SenderId && t.IsVisible);
        problem.SenderIsExpert   = titles.Any(t => t.Kind == "expert");
        problem.SenderIsOfficial = titles.Any(t => t.Kind == "official");
        problem.SenderTitles = titles.Select(t => new UserTitleDto
        {
            Id = t.Id, UserId = t.UserId, Label = t.Label, Kind = t.Kind,
            Color = t.Color, Icon = t.Icon, IsVisible = t.IsVisible, AssignedAt = t.AssignedAt
        }).ToList();
    }

    private void EnrichBadges(List<ProblemDetailDto> problems)
    {
        if (problems.Count == 0) return;

        var senderIds = problems.Select(p => p.SenderId).Distinct().ToList();

        var allTitles = _userTitleDal.GetAll(t => senderIds.Contains(t.UserId) && t.IsVisible);
        var titleMap = allTitles.GroupBy(t => t.UserId).ToDictionary(
            g => g.Key,
            g => g.ToList());

        foreach (var p in problems)
        {
            var titles = titleMap.GetValueOrDefault(p.SenderId, new List<UserTitle>());
            p.SenderIsExpert   = titles.Any(t => t.Kind == "expert");
            p.SenderIsOfficial = titles.Any(t => t.Kind == "official");
            p.SenderTitles = titles.Select(t => new UserTitleDto
            {
                Id = t.Id, UserId = t.UserId, Label = t.Label, Kind = t.Kind,
                Color = t.Color, Icon = t.Icon, IsVisible = t.IsVisible, AssignedAt = t.AssignedAt
            }).ToList();
        }
    }

    private List<OfficialResponseDto> LoadOfficialResponses(int problemId)
    {
        var responses = _officialResponseDal.GetDetails(r => r.ProblemId == problemId);
        if (responses.Count > 0)
        {
            var authorIds = responses.Select(r => r.AuthorUserId).Distinct().ToList();
            var allTitles = _userTitleDal.GetAll(t => authorIds.Contains(t.UserId) && t.IsVisible);
            var titleMap = allTitles.GroupBy(t => t.UserId).ToDictionary(
                g => g.Key,
                g => g.Select(t => new UserTitleDto
                {
                    Id = t.Id, UserId = t.UserId, Label = t.Label, Kind = t.Kind,
                    Color = t.Color, Icon = t.Icon, IsVisible = t.IsVisible, AssignedAt = t.AssignedAt
                }).ToList());

            foreach (var r in responses)
                r.AuthorTitles = titleMap.GetValueOrDefault(r.AuthorUserId, new List<UserTitleDto>());
        }
        return responses;
    }

    public IDataResult<List<ProblemParticipantDto>> GetParticipants(int problemId)
    {
        // Solution yazarları
        var solutions = _solutionDal.GetAll(s => s.ProblemId == problemId && !s.IsDeleted);
        var solutionSenderIds = solutions.Select(s => (UserId: s.SenderId, Role: "solution_author")).ToList();

        // Yorum yazarları — çözümlere bağlı
        var solutionIds = solutions.Select(s => s.Id).ToList();
        var commentSenderIds = _commentDal
            .GetAll(c => solutionIds.Contains(c.SolutionId) && !c.IsDeleted)
            .Select(c => (UserId: c.SenderId, Role: "commenter"))
            .ToList();

        // Upvoterlar
        var upvoterIds = _problemUpvoteDal
            .GetAll(u => u.ProblemId == problemId)
            .Select(u => (UserId: u.UserId, Role: "upvoter"))
            .ToList();

        // Birleştir, kullanıcı başına ilk rolü koru
        var allParticipants = solutionSenderIds
            .Concat(commentSenderIds)
            .Concat(upvoterIds)
            .GroupBy(x => x.UserId)
            .Select(g => (UserId: g.Key, Role: g.First().Role))
            .ToList();

        if (!allParticipants.Any())
            return new SuccessDataResult<List<ProblemParticipantDto>>(new List<ProblemParticipantDto>());

        var userIds = allParticipants.Select(x => x.UserId).ToList();
        var users = _userDal.GetAll(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id);

        var result = allParticipants.Select(p =>
        {
            users.TryGetValue(p.UserId, out var user);
            return new ProblemParticipantDto
            {
                UserId = p.UserId,
                Username = user?.UserName ?? "",
                ProfileImageUrl = user?.ProfileImageUrl,
                Role = p.Role
            };
        }).ToList();

        return new SuccessDataResult<List<ProblemParticipantDto>>(result);
    }
}
