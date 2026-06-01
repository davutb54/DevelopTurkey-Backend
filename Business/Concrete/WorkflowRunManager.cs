using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class WorkflowRunManager : IWorkflowRunService
{
    private readonly IWorkflowRunDal _runDal;
    private readonly INodeRunDal _nodeRunDal;
    private readonly IActionRunDal _actionRunDal;

    public WorkflowRunManager(
        IWorkflowRunDal runDal,
        INodeRunDal nodeRunDal,
        IActionRunDal actionRunDal)
    {
        _runDal = runDal;
        _nodeRunDal = nodeRunDal;
        _actionRunDal = actionRunDal;
    }

    public IDataResult<WorkflowRun> GetById(Guid id)
    {
        var run = _runDal.Get(r => r.Id == id);
        if (run is null) return new ErrorDataResult<WorkflowRun>(default, "WorkflowRun bulunamadı.");
        return new SuccessDataResult<WorkflowRun>(run);
    }

    public IDataResult<List<WorkflowRun>> GetByDefinition(int definitionId, int page = 1, int pageSize = 20)
    {
        var list = _runDal.GetByDefinition(definitionId, page, pageSize);
        return new SuccessDataResult<List<WorkflowRun>>(list);
    }

    public IDataResult<List<WorkflowRunSummaryDto>> GetSummaryByDefinition(int definitionId, int page = 1, int pageSize = 20)
    {
        var list = _runDal.GetByDefinition(definitionId, page, pageSize);
        var summaries = list.Select(r => new WorkflowRunSummaryDto
        {
            RunId              = r.Id.ToString(),
            DefinitionId       = r.DefinitionId,
            TriggerEvent       = r.TriggerEvent,
            TriggeredByUserId  = r.TriggeredByUserId,
            Status             = r.Status,
            IsDryRun           = r.IsDryRun,
            StartedAt          = r.StartedAt,
            EndedAt            = r.EndedAt,
            DurationMs         = r.DurationMs,
            ErrorMessage       = r.ErrorMessage,
            NodeRunCount       = _nodeRunDal.GetByRun(r.Id).Count,
        }).ToList();
        return new SuccessDataResult<List<WorkflowRunSummaryDto>>(summaries);
    }

    public IDataResult<WorkflowRunDetailDto> GetDetail(Guid runId)
    {
        var run = _runDal.Get(r => r.Id == runId);
        if (run is null) return new ErrorDataResult<WorkflowRunDetailDto>(default!, "WorkflowRun bulunamadı.");

        var nodeRuns = _nodeRunDal.GetByRun(runId);
        var nodeRunDtos = nodeRuns.Select(nr =>
        {
            var actionRuns = _actionRunDal.GetByNodeRun(nr.Id);
            return new NodeRunSummaryDto
            {
                Id           = nr.Id.ToString(),
                NodeId       = nr.NodeId,
                NodeType     = nr.NodeType,
                Status       = nr.Status,
                StartedAt    = nr.StartedAt,
                EndedAt      = nr.EndedAt,
                ErrorMessage = nr.ErrorMessage,
                ActionRuns   = actionRuns.Select(ar => new ActionRunSummaryDto
                {
                    Id         = ar.Id.ToString(),
                    ActionCode = ar.ActionCode,
                    Status     = ar.Status,
                    RetryCount = ar.RetryCount,
                    ResultJson = ar.ResultJson,
                    LastError  = ar.LastError,
                    StartedAt  = ar.StartedAt,
                    EndedAt    = ar.EndedAt,
                }).ToList(),
            };
        }).ToList();

        var detail = new WorkflowRunDetailDto
        {
            RunId             = run.Id.ToString(),
            DefinitionId      = run.DefinitionId,
            TriggerEvent      = run.TriggerEvent,
            InstitutionId     = run.InstitutionId,
            TriggeredByUserId = run.TriggeredByUserId,
            Status            = run.Status,
            IsDryRun          = run.IsDryRun,
            StartedAt         = run.StartedAt,
            EndedAt           = run.EndedAt,
            DurationMs        = run.DurationMs,
            ErrorMessage      = run.ErrorMessage,
            NodeRuns          = nodeRunDtos,
        };

        return new SuccessDataResult<WorkflowRunDetailDto>(detail);
    }

    public IDataResult<List<WorkflowRun>> GetByInstitution(int institutionId, byte? status = null, int page = 1, int pageSize = 20)
    {
        var list = _runDal.GetByInstitution(institutionId, status, page, pageSize);
        return new SuccessDataResult<List<WorkflowRun>>(list);
    }

    public IDataResult<int> CountByInstitution(int institutionId, byte? status = null)
    {
        var count = _runDal.CountByInstitution(institutionId, status);
        return new SuccessDataResult<int>(count);
    }

    public IDataResult<List<NodeRun>> GetNodeRuns(Guid runId)
    {
        var list = _nodeRunDal.GetByRun(runId);
        return new SuccessDataResult<List<NodeRun>>(list);
    }

    public IDataResult<List<ActionRun>> GetActionRuns(Guid nodeRunId)
    {
        var list = _actionRunDal.GetByNodeRun(nodeRunId);
        return new SuccessDataResult<List<ActionRun>>(list);
    }

    public IDataResult<List<WorkflowTimelineItemDto>> GetTimeline(Guid runId)
    {
        var nodeRuns = _nodeRunDal.GetByRun(runId);
        var timeline = new List<WorkflowTimelineItemDto>();

        foreach (var nr in nodeRuns)
        {
            timeline.Add(new WorkflowTimelineItemDto
            {
                Type      = "node",
                Id        = nr.Id.ToString(),
                NodeId    = nr.NodeId,
                NodeType  = nr.NodeType,
                Status    = nr.Status,
                StartedAt = nr.StartedAt,
                EndedAt   = nr.EndedAt,
                ErrorMessage = nr.ErrorMessage,
            });

            var actionRuns = _actionRunDal.GetByNodeRun(nr.Id);
            foreach (var ar in actionRuns)
            {
                timeline.Add(new WorkflowTimelineItemDto
                {
                    Type       = "action",
                    Id         = ar.Id.ToString(),
                    ParentId   = nr.Id.ToString(),
                    ActionCode = ar.ActionCode,
                    Status     = ar.Status,
                    StartedAt  = ar.StartedAt,
                    EndedAt    = ar.EndedAt,
                    ErrorMessage = ar.LastError,
                    RetryCount = ar.RetryCount,
                });
            }
        }

        // Zaman çizelgesi: StartedAt artan sıralı
        timeline.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
        return new SuccessDataResult<List<WorkflowTimelineItemDto>>(timeline);
    }

    public IResult UpdateRunStatus(Guid runId, byte status, string? errorMessage = null)
    {
        var run = _runDal.Get(r => r.Id == runId);
        if (run is null) return new ErrorResult("WorkflowRun bulunamadı.");

        run.Status = status;
        if (errorMessage is not null)
            run.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;

        if (status is WorkflowRunStatus.Succeeded or WorkflowRunStatus.Failed
                   or WorkflowRunStatus.Partial or WorkflowRunStatus.Canceled
                   or WorkflowRunStatus.Timeout)
        {
            run.EndedAt = DateTime.UtcNow;
            run.DurationMs = (long)(run.EndedAt.Value - run.StartedAt).TotalMilliseconds;
        }

        _runDal.Update(run);
        return new SuccessResult();
    }
}
