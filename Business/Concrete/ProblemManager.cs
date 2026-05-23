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

    public ProblemManager(IProblemDal problemDal, ILogService logService, ISolutionDal solutionDal, ICommentDal commentDal, IProblemTopicDal problemTopicDal, IClientContext clientContext, IMemoryCache cache, INotificationService notificationService, IProblemFollowService problemFollowService, ITopicFollowService topicFollowService, IUserService userService, ITopicFollowDal topicFollowDal, IInstitutionFeatureService featureService, IMentionService mentionService, IWorkflowEventBus eventBus, ICapabilityResolver capabilityResolver)
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
        var problems = _problemDal.GetProblemsDetails(problem => problem.IsDeleted == false);
        var filteredProblems = problems.Where(p => p.Topics != null && p.Topics.Any(t => t.Id == topicId)).ToList();
        return new SuccessDataResult<List<ProblemDetailDto>>(filteredProblems);
    }

    public IDataResult<List<ProblemDetailDto>> GetBySender(int senderId)
    {
        return new SuccessDataResult<List<ProblemDetailDto>>(_problemDal.GetProblemsDetails(problem => problem.SenderId == senderId));
    }

    public IDataResult<List<ProblemDetailDto>> GetIsHighlighted()
    {
        return new SuccessDataResult<List<ProblemDetailDto>>(_problemDal.GetProblemsDetails(problem => problem.IsHighlighted));
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
        var isModerator = _capabilityResolver.Allows(currentUserId.GetValueOrDefault(), "moderation.problem_moderate");
        var existingProblem = _problemDal.Get(p => p.Id == problem.Id);

        if (existingProblem == null)
        {
            return new ErrorResult("Kayıt bulunamadı");
        }

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
        var isModerator = _capabilityResolver.Allows(currentUserId.GetValueOrDefault(), "moderation.problem_delete");

        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");

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

    public IDataResult<List<ProblemDetailDto>> GetAllForAdmin()
    {
        var problems = _problemDal.GetProblemsDetails(p => p.IsDeleted == false);
        return new SuccessDataResult<List<ProblemDetailDto>>(problems.OrderByDescending(p => p.SendDate).ToList());
    }

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
}
