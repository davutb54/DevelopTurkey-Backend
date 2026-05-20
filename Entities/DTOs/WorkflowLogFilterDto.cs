namespace Entities.DTOs;

public class WorkflowLogFilterDto
{
    public int? RuleId { get; set; }
    public string? TriggerEvent { get; set; }

    /// <summary>success | partial | failed | error</summary>
    public string? Status { get; set; }

    public int? TriggeredByUserId { get; set; }
    public int? InstitutionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>RuleName veya TriggerEvent içinde serbest arama</summary>
    public string? SearchText { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
