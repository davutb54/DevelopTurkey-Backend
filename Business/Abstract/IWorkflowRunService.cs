using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface IWorkflowRunService
{
    IDataResult<WorkflowRun> GetById(Guid id);
    IDataResult<WorkflowRunDetailDto> GetDetail(Guid runId);
    IDataResult<List<WorkflowRunSummaryDto>> GetSummaryByDefinition(int definitionId, int page = 1, int pageSize = 20);
    IDataResult<List<WorkflowRun>> GetByDefinition(int definitionId, int page = 1, int pageSize = 20);
    IDataResult<List<WorkflowRun>> GetByInstitution(int institutionId, byte? status = null, int page = 1, int pageSize = 20);
    IDataResult<int> CountByInstitution(int institutionId, byte? status = null);

    IDataResult<List<NodeRun>> GetNodeRuns(Guid runId);
    IDataResult<List<ActionRun>> GetActionRuns(Guid nodeRunId);

    /// <summary>Bir WorkflowRun'ın tüm NodeRun + ActionRun zaman çizelgesi</summary>
    IDataResult<List<WorkflowTimelineItemDto>> GetTimeline(Guid runId);

    IResult UpdateRunStatus(Guid runId, byte status, string? errorMessage = null);
}

/// <summary>Timeline endpoint için DTO — her node/action bir satır</summary>
public class WorkflowTimelineItemDto
{
    public string Type { get; set; } = string.Empty;  // "node" | "action"
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? NodeId { get; set; }
    public string? NodeType { get; set; }
    public string? ActionCode { get; set; }
    public byte Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
