using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.DTOs.Metrics;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Business.Concrete;

public class MetricsManager : IMetricsService
{
    private readonly IUserDal _userDal;
    private readonly IProblemDal _problemDal;
    private readonly ISolutionDal _solutionDal;
    private readonly ICommentDal _commentDal;
    private readonly IInstitutionDal _institutionDal;
    private readonly ICapabilitySnapshot _snapshot;
    private readonly IMemoryCache _cache;
    private readonly DevelopTurkeyContext _db;
    private static readonly DateTime _startedAt = DateTime.UtcNow;

    public MetricsManager(
        IUserDal userDal,
        IProblemDal problemDal,
        ISolutionDal solutionDal,
        ICommentDal commentDal,
        IInstitutionDal institutionDal,
        ICapabilitySnapshot snapshot,
        IMemoryCache cache,
        DevelopTurkeyContext db)
    {
        _userDal = userDal;
        _problemDal = problemDal;
        _solutionDal = solutionDal;
        _commentDal = commentDal;
        _institutionDal = institutionDal;
        _snapshot = snapshot;
        _cache = cache;
        _db = db;
    }

    public IDataResult<OverviewMetricsDto> GetOverview()
    {
        const string key = "metrics:overview";
        if (!_cache.TryGetValue(key, out OverviewMetricsDto? dto))
        {
            var now = DateTime.UtcNow;
            var last24h = now.AddHours(-24);
            var last7d = now.AddDays(-7);

            var wfLast24h = _db.WorkflowLogs
                .Where(w => w.ExecutedAt >= last24h)
                .Select(w => w.Status)
                .ToList();

            dto = new OverviewMetricsDto
            {
                TotalUsers         = _userDal.Count(),
                NewUsersLast7Days  = _userDal.Count(u => u.RegisterDate >= last7d),
                TotalProblems      = _problemDal.Count(),
                TotalSolutions     = _solutionDal.Count(),
                TotalComments      = _commentDal.Count(),
                TotalCapabilityGrants = _db.UserCapabilities.Count(uc => uc.Status == 1),
                ActiveWorkflowDefs = _db.DynamicRules.Count(r => r.IsActive),
                WorkflowRunsLast24h = wfLast24h.Count,
                WorkflowSuccessRateLast24h = wfLast24h.Count > 0
                    ? Math.Round(100.0 * wfLast24h.Count(s => s == "success") / wfLast24h.Count, 1)
                    : 0,
                SnapshotEntryCount = _snapshot.Count,
                SnapshotLoadedAt   = _snapshot.LoadedAt,
                UptimeHours        = Math.Round((now - _startedAt).TotalHours, 2),
                RamUsageMb         = Math.Round(Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0, 1),
                BannedUsers        = _userDal.Count(u => u.IsBanned),
            };

            _cache.Set(key, dto, TimeSpan.FromMinutes(5));
        }

        return new SuccessDataResult<OverviewMetricsDto>(dto!);
    }

    public IDataResult<CapabilityMetricsDto> GetCapabilityMetrics(DateTime? from, DateTime? to)
    {
        var f = from ?? DateTime.UtcNow.AddDays(-30);
        var t = to ?? DateTime.UtcNow;
        string key = $"metrics:caps:{f:yyyyMMdd}:{t:yyyyMMdd}";

        if (!_cache.TryGetValue(key, out CapabilityMetricsDto? dto))
        {
            // Grant/Revoke trend (daily)
            var auditRows = _db.CapabilityAuditLogs
                .Where(a => (a.Action == "grant" || a.Action == "revoke") && a.CreatedAt >= f && a.CreatedAt <= t)
                .Select(a => new { a.Action, a.CreatedAt })
                .ToList();

            var days = (int)(t - f).TotalDays + 1;
            var trend = Enumerable.Range(0, days).Select(i =>
            {
                var date = f.AddDays(i).Date;
                return new DailyGrantRevokeDto
                {
                    Date    = date.ToString("dd MMM"),
                    Grants  = auditRows.Count(a => a.Action == "grant" && a.CreatedAt.Date == date),
                    Revokes = auditRows.Count(a => a.Action == "revoke" && a.CreatedAt.Date == date),
                };
            }).ToList();

            // Top capabilities by grant count
            var capJoin = (from uc in _db.UserCapabilities
                           join c in _db.Capabilities on uc.CapabilityId equals c.Id
                           where uc.Status == 1
                           group c by new { c.Code, c.Category } into g
                           orderby g.Count() descending
                           select new CapabilityUsageDto
                           {
                               Code = g.Key.Code,
                               Category = g.Key.Category ?? string.Empty,
                               GrantCount = g.Count()
                           }).Take(20).ToList();

            // Top users by capability count
            var topUsers = (from uc in _db.UserCapabilities
                            join u in _db.Users on uc.UserId equals u.Id
                            where uc.Status == 1
                            group u by new { u.Id, u.UserName } into g
                            orderby g.Count() descending
                            select new UserCapabilityCountDto
                            {
                                UserId = g.Key.Id,
                                UserName = g.Key.UserName,
                                CapabilityCount = g.Count()
                            }).Take(20).ToList();

            // Category breakdown
            var catBreakdown = (from uc in _db.UserCapabilities
                                join c in _db.Capabilities on uc.CapabilityId equals c.Id
                                where uc.Status == 1
                                group c by c.Category into g
                                select new CategoryCountDto
                                {
                                    Category = g.Key ?? "other",
                                    Count = g.Count()
                                }).ToList();

            var totalActive = _db.UserCapabilities.Count(uc => uc.Status == 1);
            var distinctUsers = _db.UserCapabilities.Where(uc => uc.Status == 1).Select(uc => uc.UserId).Distinct().Count();

            dto = new CapabilityMetricsDto
            {
                SnapshotEntryCount = _snapshot.Count,
                UniqueUsers        = distinctUsers,
                AvgCapsPerUser     = distinctUsers > 0 ? Math.Round((double)totalActive / distinctUsers, 1) : 0,
                SnapshotLoadedAt   = _snapshot.LoadedAt,
                GrantRevokeTrend   = trend,
                TopCapabilities    = capJoin,
                TopUsers           = topUsers,
                CategoryBreakdown  = catBreakdown,
            };

            _cache.Set(key, dto, TimeSpan.FromMinutes(10));
        }

        return new SuccessDataResult<CapabilityMetricsDto>(dto!);
    }

    public IDataResult<WorkflowMetricsDto> GetWorkflowMetrics(DateTime? from, DateTime? to)
    {
        var f = from ?? DateTime.UtcNow.AddDays(-30);
        var t = to ?? DateTime.UtcNow;
        string key = $"metrics:workflow:{f:yyyyMMdd}:{t:yyyyMMdd}";

        if (!_cache.TryGetValue(key, out WorkflowMetricsDto? dto))
        {
            var logs = _db.WorkflowLogs
                .Where(w => w.ExecutedAt >= f && w.ExecutedAt <= t)
                .Select(w => new { w.Id, w.RuleName, w.TriggerEvent, w.Status, w.ErrorMessage, w.DurationMs, w.ExecutedAt })
                .ToList();

            var days = (int)(t - f).TotalDays + 1;
            var trend = Enumerable.Range(0, days).Select(i =>
            {
                var date = f.AddDays(i).Date;
                var dayLogs = logs.Where(l => l.ExecutedAt.Date == date).ToList();
                return new DailyWorkflowRunDto
                {
                    Date    = date.ToString("dd MMM"),
                    Total   = dayLogs.Count,
                    Success = dayLogs.Count(l => l.Status == "success"),
                    Failed  = dayLogs.Count(l => l.Status is "failed" or "error"),
                };
            }).ToList();

            var topTriggers = logs
                .GroupBy(l => l.TriggerEvent)
                .Select(g => new TriggerCountDto { TriggerEvent = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var recentFailed = logs
                .Where(l => l.Status is "failed" or "error")
                .OrderByDescending(l => l.ExecutedAt)
                .Take(50)
                .Select(l => new WorkflowRunSummaryDto
                {
                    Id = l.Id, RuleName = l.RuleName, TriggerEvent = l.TriggerEvent,
                    Status = l.Status, ErrorMessage = l.ErrorMessage,
                    DurationMs = l.DurationMs, ExecutedAt = l.ExecutedAt,
                }).ToList();

            var total = logs.Count;
            var success = logs.Count(l => l.Status == "success");

            dto = new WorkflowMetricsDto
            {
                RunCountTrend  = trend,
                TotalRuns      = total,
                SuccessCount   = success,
                FailedCount    = logs.Count(l => l.Status is "failed" or "error"),
                PartialCount   = logs.Count(l => l.Status == "partial"),
                SuccessRate    = total > 0 ? Math.Round(100.0 * success / total, 1) : 0,
                AvgDurationMs  = total > 0 ? Math.Round(logs.Average(l => (double)l.DurationMs), 0) : 0,
                TopTriggers    = topTriggers,
                RecentFailedRuns = recentFailed,
            };

            _cache.Set(key, dto, TimeSpan.FromMinutes(10));
        }

        return new SuccessDataResult<WorkflowMetricsDto>(dto!);
    }

    public IDataResult<UserMetricsDto> GetUserMetrics(DateTime? from, DateTime? to)
    {
        var f = from ?? DateTime.UtcNow.AddDays(-30);
        var t = to ?? DateTime.UtcNow;
        string key = $"metrics:users:{f:yyyyMMdd}:{t:yyyyMMdd}";

        if (!_cache.TryGetValue(key, out UserMetricsDto? dto))
        {
            var users = _userDal.GetAll();
            var institutions = _institutionDal.GetAll();
            var problems = _problemDal.GetAll();
            var solutions = _solutionDal.GetAll();
            var comments = _commentDal.GetAll();

            // New user trend
            var days = (int)(t - f).TotalDays + 1;
            var trend = Enumerable.Range(0, days).Select(i =>
            {
                var date = f.AddDays(i).Date;
                return new DailyUserRegistrationDto2
                {
                    Date  = date.ToString("dd MMM"),
                    Count = users.Count(u => u.RegisterDate.Date == date),
                };
            }).ToList();

            // Top contributors (problem + solution + comment count)
            var topContributors = users.Select(u => new ContributorDto
            {
                UserId   = u.Id,
                UserName = u.UserName,
                ProblemCount  = problems.Count(p => p.SenderId == u.Id),
                SolutionCount = solutions.Count(s => s.SenderId == u.Id),
                CommentCount  = comments.Count(c => c.SenderId == u.Id),
            })
            .Select(c => { c.Total = c.ProblemCount + c.SolutionCount + c.CommentCount; return c; })
            .Where(c => c.Total > 0)
            .OrderByDescending(c => c.Total)
            .Take(20)
            .ToList();

            // Per institution
            var perInstitution = users
                .GroupBy(u => u.InstitutionId)
                .Select(g => new InstitutionUserCountDto
                {
                    InstitutionName = institutions.FirstOrDefault(i => i.Id == g.Key)?.Name ?? "Bilinmiyor",
                    UserCount = g.Count(),
                })
                .OrderByDescending(x => x.UserCount)
                .ToList();

            var warnedUserIds = _db.UserWarnings
                .Where(w => w.IsActive)
                .Select(w => w.UserId)
                .Distinct()
                .ToHashSet();

            dto = new UserMetricsDto
            {
                TotalUsers       = users.Count,
                BannedUsers      = users.Count(u => u.IsBanned),
                WarnedUsers      = warnedUserIds.Count,
                UnverifiedUsers  = users.Count(u => !u.IsEmailVerified && !u.IsDeleted),
                NewUserTrend     = trend,
                TopContributors  = topContributors,
                PerInstitution   = perInstitution,
            };

            _cache.Set(key, dto, TimeSpan.FromMinutes(10));
        }

        return new SuccessDataResult<UserMetricsDto>(dto!);
    }

    public IDataResult<PagedResult<CapabilityAuditLogDto>> GetAuditLog(CapabilityAuditFilterDto filter)
    {
        var query = _db.CapabilityAuditLogs.AsQueryable();

        if (filter.ActorUserId.HasValue)  query = query.Where(a => a.ActorUserId == filter.ActorUserId.Value);
        if (filter.TargetUserId.HasValue) query = query.Where(a => a.TargetUserId == filter.TargetUserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Action)) query = query.Where(a => a.Action == filter.Action);
        if (filter.From.HasValue) query = query.Where(a => a.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)   query = query.Where(a => a.CreatedAt <= filter.To.Value);

        var total = query.Count();
        var items = query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new CapabilityAuditLogDto
            {
                Id = a.Id, ActorUserId = a.ActorUserId, TargetUserId = a.TargetUserId,
                Action = a.Action, PayloadJson = a.PayloadJson, CreatedAt = a.CreatedAt,
            })
            .ToList();

        return new SuccessDataResult<PagedResult<CapabilityAuditLogDto>>(new PagedResult<CapabilityAuditLogDto>
        {
            Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize,
        });
    }
}
