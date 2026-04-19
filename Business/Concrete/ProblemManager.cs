using System.Linq;
using Business.Abstract;
using Core.Entities.Concrete;
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

    public ProblemManager(IProblemDal problemDal, ILogService logService, ISolutionDal solutionDal, ICommentDal commentDal, IProblemTopicDal problemTopicDal, IClientContext clientContext, IMemoryCache cache, INotificationService notificationService)
    {
        _problemDal = problemDal;
        _logService = logService;
        _solutionDal = solutionDal;
        _commentDal = commentDal;
        _problemTopicDal = problemTopicDal;
        _clientContext = clientContext;
        _cache = cache;
        _notificationService = notificationService;
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
        return new SuccessResult(Messages.ProblemAdded);
    }

    public IResult Update(Problem problem, List<int> topicIds)
    {
        var currentUserId = _clientContext.GetUserId();
        var isAdmin = _clientContext.GetRoles().Contains("Admin");
        var existingProblem = _problemDal.Get(p => p.Id == problem.Id);

        if (existingProblem == null)
        {
            return new ErrorResult("Kayıt bulunamadı");
        }

        // TODO: İleride Moderator rolü (Örn: IsOfficial) eklendiğinde, moderatörün 
        // kendi kurumuna (InstitutionId) ait olmayan problemleri güncellemesi engellenmelidir.
        // Örn: var institutionId = _clientContext.GetInstitutionId();
        // if (!isAdmin && isOfficial && existingProblem.InstitutionId != institutionId) return new ErrorResult(Messages.AuthorizationDenied); 

        if (!isAdmin && existingProblem.SenderId != currentUserId)
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

        // Yalnızca Adminlerin müdahale edebileceği ayarlar; eğer kişi Admin değilse Database'dekini eziyoruz.
        if (!isAdmin)
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
        return new SuccessResult(Messages.ProblemUpdated);
    }

    public IResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var isAdmin = _clientContext.GetRoles().Contains("Admin");

        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");

        // TODO: İleride Moderator rolü (Örn: IsOfficial) eklendiğinde, moderatörün 
        // kendi kurumuna (InstitutionId) ait olmayan problemleri silmesi engellenmelidir.
        // Örn: var institutionId = _clientContext.GetInstitutionId();
        // if (!isAdmin && isOfficial && problem.InstitutionId != institutionId) return new ErrorResult(Messages.AuthorizationDenied);

        if (!isAdmin && problem.SenderId != currentUserId)
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

        if (isAdmin && problem.SenderId != currentUserId)
        {
            _logService.LogWarning("AdminAction", "Delete", $"Problem GÖREVLİ tarafından silindi - ID: {id} (Alt Çözüm ve Yorumlarıyla Birlikte)");
            try
            {
                _notificationService.Add(new Notification
                {
                    UserId = problem.SenderId,
                    Title = "Bir içeriğiniz kaldırıldı",
                    Message = $"\"{problem.Title}\" başlıklı sorunuz platform kurallarına aykırı olduğu için kaldırıldı.",
                    Type = "ContentRemoved",
                    ReferenceLink = null
                });
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }
        else
        {
            _logService.LogWarning("Content", "Delete", $"Problem kullanıcı tarafından silindi - ID: {id} (Alt Çözüm ve Yorumlarıyla Birlikte)");
        }

        return new SuccessResult(Messages.ProblemDeleted);
    }

    public IDataResult<List<ProblemDetailDto>> GetList(ProblemFilterDto filterDto, int institutionId)
    {
        var problems = _problemDal.GetProblemsDetails(p =>
            (p.IsDeleted == false) &&
            (institutionId == 0 || p.InstitutionId == institutionId) &&
            (!filterDto.CityCode.HasValue || p.CityCode == filterDto.CityCode.Value) &&
            (string.IsNullOrEmpty(filterDto.SearchText) || p.Title.Contains(filterDto.SearchText) || p.Description.Contains(filterDto.SearchText))
        );

        if (filterDto.TopicId.HasValue && filterDto.TopicId.Value > 0)
        {
            problems = problems.Where(p => p.Topics != null && p.Topics.Any(t => t.Id == filterDto.TopicId.Value)).ToList();
        }

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
        _logService.LogInfo("Moderation", "Report", $"Problem raporlandı - ID: {problem.Id}");
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
        _logService.LogInfo("AdminAction", "Highlight", $"Problem {(problem.IsHighlighted ? "vurgulandı" : "vurgulama kaldırıldı")} - ID: {problem.Id}");
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
        return new SuccessResult($"Problem (ID: {problem.Id}) {(problem.IsResolved ? "çözüldü" : "çözülmedi olarak işaretlendi")}.");
    }

    public IResult ResolveProblem(int id)
    {
        var problem = _problemDal.Get(p => p.Id == id);
        if (problem == null) return new ErrorResult("Kayıt bulunamadı");
        problem.IsResolved = true;
        _problemDal.Update(problem);
        _logService.LogInfo("Content", "Resolve", $"Problem çözüldü - ID: {problem.Id}");
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
                    _notificationService.Add(new Notification
                    {
                        UserId = problem.SenderId,
                        Title = "Sorunuzdan bir kategori kaldırıldı",
                        Message = "Yöneticiler, paylaştığınız sorundan uygunsuz bir kategoriyi kaldırdı.",
                        Type = "ContentModified",
                        ReferenceLink = $"/problem/{problemId}"
                    });
                }
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }

            return new SuccessResult("Kategori sorundan başarıyla kaldırıldı.");
        }
        return new ErrorResult("Bu sorunda böyle bir kategori bulunamadı.");
    }
}