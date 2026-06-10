using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using System.Linq.Expressions;
using Core.Entities.Constants;

namespace DataAccess.Concrete.EntityFramework;

public class EfProblemDal : EfEntityRepositoryBase<Problem, DevelopTurkeyContext>, IProblemDal
{
    public List<ProblemDetailDto> GetProblemsDetails(Expression<Func<ProblemDetailDto, bool>>? filter = null)
    {
        using DevelopTurkeyContext context = new DevelopTurkeyContext();

        // SQL-only projection: correlated scalar subqueries translate to a single SQL query.
        // Non-translatable operations (Split, ConstantData.GetCity, Topics) are deferred to in-memory steps below.
        var rawList = (from p in context.Problems
                       join u in context.Users on p.SenderId equals u.Id
                       where p.IsDeleted == false
                       select new
                       {
                           p.Id,
                           p.Title,
                           p.Description,
                           p.CityCode,
                           p.CustomHierarchyId,
                           p.Address,
                           p.Latitude,
                           p.Longitude,
                           p.IsHighlighted,
                           p.IsReported,
                           p.IsDeleted,
                           p.SenderId,
                           p.ImageUrls,
                           SenderUsername     = u.UserName,
                           SenderImageUrl     = u.ProfileImageUrl,
                           p.SendDate,
                           p.ViewCount,
                           SolutionCount      = context.Solutions.Count(s => s.ProblemId == p.Id),
                           IsResolvedByExpert = context.Solutions.Any(s => s.ProblemId == p.Id && s.ExpertApprovalStatus == 1),
                           p.IsResolved,
                           p.InstitutionId,
                           UpvoteCount        = context.ProblemUpvotes.Count(uv => uv.ProblemId == p.Id),
                           FollowerCount      = context.ProblemFollowers.Count(f => f.ProblemId == p.Id),
                           p.IsClosed,
                           p.ClosedAt,
                           p.ClosedByUserId,
                           p.CloseReason,
                           p.IsHidden
                       }).ToList();

        var problems = rawList.Select(x => new ProblemDetailDto
        {
            Id                = x.Id,
            Title             = x.Title,
            Description       = x.Description,
            CityCode          = x.CityCode,
            CustomHierarchyId = x.CustomHierarchyId,
            Address           = x.Address,
            Latitude          = x.Latitude,
            Longitude         = x.Longitude,
            IsHighlighted     = x.IsHighlighted,
            IsReported        = x.IsReported,
            IsDeleted         = x.IsDeleted,
            SenderId          = x.SenderId,
            ImageUrls         = x.ImageUrls != null
                                    ? x.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                                    : new List<string>(),
            SenderUsername    = x.SenderUsername,
            CityName          = ConstantData.GetCity(x.CityCode).Text,
            SenderIsExpert    = false,
            SendDate          = x.SendDate,
            ViewCount         = x.ViewCount,
            SolutionCount     = x.SolutionCount,
            SenderIsOfficial  = false,
            SenderImageUrl    = x.SenderImageUrl,
            IsResolvedByExpert = x.IsResolvedByExpert,
            IsResolved        = x.IsResolved,
            InstitutionId     = x.InstitutionId,
            UpvoteCount       = x.UpvoteCount,
            FollowerCount     = x.FollowerCount,
            IsClosed          = x.IsClosed,
            ClosedAt          = x.ClosedAt,
            ClosedByUserId    = x.ClosedByUserId,
            CloseReason       = x.CloseReason,
            IsHidden          = x.IsHidden,
            Topics            = new List<TopicDto>()
        }).ToList();

        var result = filter == null ? problems : problems.Where(filter.Compile()).ToList();
        LoadTopics(context, result);
        return result;
    }

    public ProblemDetailDto GetProblemDetail(int id)
    {
        using DevelopTurkeyContext context = new DevelopTurkeyContext();

        var raw = (from p in context.Problems
                   join u in context.Users on p.SenderId equals u.Id
                   where p.IsDeleted == false && p.Id == id
                   select new
                   {
                       p.Id,
                       p.Title,
                       p.Description,
                       p.CityCode,
                       p.CustomHierarchyId,
                       p.Address,
                       p.Latitude,
                       p.Longitude,
                       p.IsHighlighted,
                       p.IsReported,
                       p.IsDeleted,
                       p.SenderId,
                       p.ImageUrls,
                       SenderUsername     = u.UserName,
                       SenderImageUrl     = u.ProfileImageUrl,
                       p.SendDate,
                       p.ViewCount,
                       SolutionCount      = context.Solutions.Count(s => s.ProblemId == p.Id),
                       IsResolvedByExpert = context.Solutions.Any(s => s.ProblemId == p.Id && s.ExpertApprovalStatus == 1),
                       p.IsResolved,
                       p.InstitutionId,
                       UpvoteCount        = context.ProblemUpvotes.Count(uv => uv.ProblemId == p.Id),
                       FollowerCount      = context.ProblemFollowers.Count(f => f.ProblemId == p.Id),
                       p.IsClosed,
                       p.ClosedAt,
                       p.ClosedByUserId,
                       p.CloseReason,
                       p.IsHidden
                   }).SingleOrDefault();

        if (raw == null) return null;

        var problem = new ProblemDetailDto
        {
            Id                = raw.Id,
            Title             = raw.Title,
            Description       = raw.Description,
            CityCode          = raw.CityCode,
            CustomHierarchyId = raw.CustomHierarchyId,
            Address           = raw.Address,
            Latitude          = raw.Latitude,
            Longitude         = raw.Longitude,
            IsHighlighted     = raw.IsHighlighted,
            IsReported        = raw.IsReported,
            IsDeleted         = raw.IsDeleted,
            SenderId          = raw.SenderId,
            ImageUrls         = raw.ImageUrls != null
                                    ? raw.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                                    : new List<string>(),
            SenderUsername    = raw.SenderUsername,
            CityName          = ConstantData.GetCity(raw.CityCode).Text,
            SenderIsExpert    = false,
            SendDate          = raw.SendDate,
            ViewCount         = raw.ViewCount,
            SolutionCount     = raw.SolutionCount,
            SenderIsOfficial  = false,
            SenderImageUrl    = raw.SenderImageUrl,
            IsResolvedByExpert = raw.IsResolvedByExpert,
            IsResolved        = raw.IsResolved,
            InstitutionId     = raw.InstitutionId,
            UpvoteCount       = raw.UpvoteCount,
            FollowerCount     = raw.FollowerCount,
            IsClosed          = raw.IsClosed,
            ClosedAt          = raw.ClosedAt,
            ClosedByUserId    = raw.ClosedByUserId,
            CloseReason       = raw.CloseReason,
            IsHidden          = raw.IsHidden,
            Topics            = new List<TopicDto>()
        };

        LoadTopics(context, new List<ProblemDetailDto> { problem });
        return problem;
    }

    public List<ProblemDetailDto> GetListByFilter(ProblemFilterDto filterDto)
    {
        using var context = new DevelopTurkeyContext();

        var query = from p in context.Problems
                    join u in context.Users on p.SenderId equals u.Id
                    where p.IsDeleted == false
                    select new { p, u };

        if (!string.IsNullOrEmpty(filterDto.SearchText))
        {
            string text = filterDto.SearchText.ToLower();
            query = query.Where(x => x.p.Title.ToLower().Contains(text) ||
                                      x.p.Description.ToLower().Contains(text));
        }

        if (filterDto.CityCode.HasValue && filterDto.CityCode.Value > 0)
            query = query.Where(x => x.p.CityCode == filterDto.CityCode.Value);

        if (filterDto.TopicId.HasValue && filterDto.TopicId.Value > 0)
        {
            int topicId = filterDto.TopicId.Value;
            query = query.Where(x => context.ProblemTopics.Any(pt => pt.ProblemId == x.p.Id && pt.TopicId == topicId));
        }

        var rawList = query.Select(x => new
        {
            x.p.Id,
            x.p.Title,
            x.p.Description,
            x.p.CityCode,
            x.p.CustomHierarchyId,
            x.p.Address,
            x.p.Latitude,
            x.p.Longitude,
            x.p.IsHighlighted,
            x.p.IsReported,
            x.p.IsDeleted,
            x.p.SenderId,
            x.p.ImageUrls,
            SenderUsername     = x.u.UserName,
            SenderImageUrl     = x.u.ProfileImageUrl,
            x.p.SendDate,
            x.p.ViewCount,
            SolutionCount      = context.Solutions.Count(s => s.ProblemId == x.p.Id),
            IsResolvedByExpert = context.Solutions.Any(s => s.ProblemId == x.p.Id && s.ExpertApprovalStatus == 1),
            x.p.IsResolved,
            x.p.InstitutionId,
            UpvoteCount        = context.ProblemUpvotes.Count(uv => uv.ProblemId == x.p.Id),
            FollowerCount      = context.ProblemFollowers.Count(f => f.ProblemId == x.p.Id),
            x.p.IsClosed,
            x.p.ClosedAt,
            x.p.ClosedByUserId,
            x.p.CloseReason,
            x.p.IsHidden
        }).OrderByDescending(x => x.ViewCount).ToList();

        var problems = rawList.Select(x => new ProblemDetailDto
        {
            Id                = x.Id,
            Title             = x.Title,
            Description       = x.Description,
            CityCode          = x.CityCode,
            CustomHierarchyId = x.CustomHierarchyId,
            Address           = x.Address,
            Latitude          = x.Latitude,
            Longitude         = x.Longitude,
            IsHighlighted     = x.IsHighlighted,
            IsReported        = x.IsReported,
            IsDeleted         = x.IsDeleted,
            SenderId          = x.SenderId,
            ImageUrls         = x.ImageUrls != null
                                    ? x.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                                    : new List<string>(),
            SenderUsername    = x.SenderUsername,
            CityName          = ConstantData.GetCity(x.CityCode).Text,
            SenderIsExpert    = false,
            SendDate          = x.SendDate,
            ViewCount         = x.ViewCount,
            SolutionCount     = x.SolutionCount,
            SenderIsOfficial  = false,
            SenderImageUrl    = x.SenderImageUrl,
            IsResolvedByExpert = x.IsResolvedByExpert,
            IsResolved        = x.IsResolved,
            InstitutionId     = x.InstitutionId,
            UpvoteCount       = x.UpvoteCount,
            FollowerCount     = x.FollowerCount,
            IsClosed          = x.IsClosed,
            ClosedAt          = x.ClosedAt,
            ClosedByUserId    = x.ClosedByUserId,
            CloseReason       = x.CloseReason,
            IsHidden          = x.IsHidden,
            Topics            = new List<TopicDto>()
        }).ToList();

        LoadTopics(context, problems);
        return problems;
    }

    // Single batch query — replaces per-row correlated collection fetch (the N+1 source).
    private static void LoadTopics(DevelopTurkeyContext context, List<ProblemDetailDto> problems)
    {
        if (problems.Count == 0) return;

        var ids = problems.Select(p => p.Id).ToList();

        var rows = (from pt in context.ProblemTopics
                    join t in context.Topics on pt.TopicId equals t.Id
                    where ids.Contains(pt.ProblemId) && t.Status == true
                    select new { pt.ProblemId, t.Id, t.Name }).ToList();

        var topicMap = rows
            .GroupBy(x => x.ProblemId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new TopicDto { Id = x.Id, Name = x.Name }).ToList());

        foreach (var p in problems)
            p.Topics = topicMap.TryGetValue(p.Id, out var topics) ? topics : new List<TopicDto>();
    }
}
