using Core.Entities;

namespace Entities.Concrete;

public class WorkflowLog : IEntity
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public int TriggeredByUserId { get; set; }

    /// <summary>success | partial | failed | error</summary>
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    /// <summary>JSON array — node bazlı trace mesajları</summary>
    public string? TraceJson { get; set; }

    public int TotalNodeCount { get; set; }
    public int ExecutedNodeCount { get; set; }
    public long DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
