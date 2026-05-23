namespace Entities.DTOs.Metrics;

// ─── Overview ────────────────────────────────────────────────
public class OverviewMetricsDto
{
    public int TotalUsers { get; set; }
    public int NewUsersLast7Days { get; set; }
    public int TotalProblems { get; set; }
    public int TotalSolutions { get; set; }
    public int TotalComments { get; set; }
    public int TotalCapabilityGrants { get; set; }
    public int ActiveWorkflowDefs { get; set; }
    public int WorkflowRunsLast24h { get; set; }
    public double WorkflowSuccessRateLast24h { get; set; }
    public int SnapshotEntryCount { get; set; }
    public DateTime SnapshotLoadedAt { get; set; }
    public double UptimeHours { get; set; }
    public double RamUsageMb { get; set; }
    public int BannedUsers { get; set; }
}

// ─── Capability Metrics ───────────────────────────────────────
public class CapabilityMetricsDto
{
    public int SnapshotEntryCount { get; set; }
    public int UniqueUsers { get; set; }
    public double AvgCapsPerUser { get; set; }
    public DateTime SnapshotLoadedAt { get; set; }
    public List<DailyGrantRevokeDto> GrantRevokeTrend { get; set; } = [];
    public List<CapabilityUsageDto> TopCapabilities { get; set; } = [];
    public List<UserCapabilityCountDto> TopUsers { get; set; } = [];
    public List<CategoryCountDto> CategoryBreakdown { get; set; } = [];
}

public class DailyGrantRevokeDto
{
    public string Date { get; set; } = string.Empty;
    public int Grants { get; set; }
    public int Revokes { get; set; }
}

public class CapabilityUsageDto
{
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int GrantCount { get; set; }
}

public class UserCapabilityCountDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int CapabilityCount { get; set; }
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ─── Workflow Metrics ─────────────────────────────────────────
public class WorkflowMetricsDto
{
    public List<DailyWorkflowRunDto> RunCountTrend { get; set; } = [];
    public int TotalRuns { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int PartialCount { get; set; }
    public double SuccessRate { get; set; }
    public double AvgDurationMs { get; set; }
    public List<TriggerCountDto> TopTriggers { get; set; } = [];
    public List<WorkflowRunSummaryDto> RecentFailedRuns { get; set; } = [];
}

public class DailyWorkflowRunDto
{
    public string Date { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
}

public class TriggerCountDto
{
    public string TriggerEvent { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WorkflowRunSummaryDto
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; }
}

// ─── User Metrics ─────────────────────────────────────────────
public class UserMetricsDto
{
    public int TotalUsers { get; set; }
    public int BannedUsers { get; set; }
    public int WarnedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public List<DailyUserRegistrationDto2> NewUserTrend { get; set; } = [];
    public List<ContributorDto> TopContributors { get; set; } = [];
    public List<InstitutionUserCountDto> PerInstitution { get; set; } = [];
}

public class DailyUserRegistrationDto2
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ContributorDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ProblemCount { get; set; }
    public int SolutionCount { get; set; }
    public int CommentCount { get; set; }
    public int Total { get; set; }
}

public class InstitutionUserCountDto
{
    public string InstitutionName { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

// ─── Audit Log ────────────────────────────────────────────────
public class CapabilityAuditFilterDto
{
    public int? ActorUserId { get; set; }
    public int? TargetUserId { get; set; }
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class CapabilityAuditLogDto
{
    public int Id { get; set; }
    public int ActorUserId { get; set; }
    public int TargetUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
