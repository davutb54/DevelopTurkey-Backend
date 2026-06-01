namespace Entities.DTOs;

public class WorkflowRunSummaryDto
{
    public string RunId { get; set; } = string.Empty;
    public int DefinitionId { get; set; }
    public string TriggerEvent { get; set; } = string.Empty;
    public int TriggeredByUserId { get; set; }
    public byte Status { get; set; }
    public bool IsDryRun { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int NodeRunCount { get; set; }
}

public class WorkflowRunDetailDto
{
    public string RunId { get; set; } = string.Empty;
    public int DefinitionId { get; set; }
    public string TriggerEvent { get; set; } = string.Empty;
    public int InstitutionId { get; set; }
    public int TriggeredByUserId { get; set; }
    public byte Status { get; set; }
    public bool IsDryRun { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public List<NodeRunSummaryDto> NodeRuns { get; set; } = [];
}

public class NodeRunSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public byte Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ActionRunSummaryDto> ActionRuns { get; set; } = [];
}

public class ActionRunSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public byte Status { get; set; }
    public int RetryCount { get; set; }
    public string? ResultJson { get; set; }
    public string? LastError { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
