using System.Text.Json;

namespace CSharpSandbox;

/// <summary>
/// Roslyn C# Script globals nesnesi — Business.Models.RuleContext'in sandbox kopyası.
/// Primitif alanlar bire bir kopyalanmıştır. Karmaşık DTO snapshot'ları JsonElement
/// olarak tutulur; script içinde .GetProperty("...").GetString() ile erişilir.
/// </summary>
public class SandboxRuleContext
{
    // ── System User ──────────────────────────────────────────────────────────
    public int SystemUserId { get; set; }
    public string UserRole { get; set; } = "User";
    public int? UserScore { get; set; }
    public int? UserProblemCount { get; set; }
    public int? InstitutionId { get; set; }
    public bool? UserIsBanned { get; set; }
    public bool? UserIsEmailVerified { get; set; }

    // ── Target User ───────────────────────────────────────────────────────────
    public int? TargetUserId { get; set; }
    public string? TargetUserRole { get; set; }
    public int? TargetUserScore { get; set; }
    public int? TargetUserInstitutionId { get; set; }
    public bool? TargetUserIsBanned { get; set; }
    public bool? TargetUserIsEmailVerified { get; set; }

    // ── Problem ───────────────────────────────────────────────────────────────
    public int? ProblemId { get; set; }
    public int? ProblemOwnerId { get; set; }
    public string? ProblemStatus { get; set; }
    public string? ProblemDifficulty { get; set; }
    public int? ProblemInstitutionId { get; set; }
    public int? ProblemViewCount { get; set; }
    public int? ProblemSolutionCount { get; set; }
    public int? ProblemUpvoteCount { get; set; }
    public int? ProblemFollowerCount { get; set; }
    public bool? ProblemIsHighlighted { get; set; }
    public bool? ProblemIsReported { get; set; }

    // ── Solution ──────────────────────────────────────────────────────────────
    public int? SolutionId { get; set; }
    public int? SolutionOwnerId { get; set; }
    public int? SolutionInstitutionId { get; set; }
    public int? SolutionVoteCount { get; set; }
    public int? SolutionApprovalStatus { get; set; }
    public bool? SolutionIsHighlighted { get; set; }
    public bool? SolutionIsReported { get; set; }

    // ── Comment / Meta ────────────────────────────────────────────────────────
    public int? CommentId { get; set; }
    public string TriggerEventName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }

    // ── Snapshots (ham JSON — .GetProperty("FieldName").GetString() ile eriş) ─
    public JsonElement? UserSnapshot { get; set; }
    public JsonElement? ProblemSnapshot { get; set; }
    public JsonElement? SolutionSnapshot { get; set; }
    public JsonElement? TargetUserSnapshot { get; set; }

    public Dictionary<string, JsonElement> Metadata { get; set; } = new();
}
