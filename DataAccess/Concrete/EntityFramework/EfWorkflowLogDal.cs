using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework;

public class EfWorkflowLogDal : EfEntityRepositoryBase<WorkflowLog, DevelopTurkeyContext>, IWorkflowLogDal
{
    public List<WorkflowLog> GetListByFilter(WorkflowLogFilterDto filter)
    {
        using var context = new DevelopTurkeyContext();
        var query = BuildQuery(context, filter);

        return query
            .OrderByDescending(l => l.ExecutedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();
    }

    public int CountByFilter(WorkflowLogFilterDto filter)
    {
        using var context = new DevelopTurkeyContext();
        return BuildQuery(context, filter).Count();
    }

    private static IQueryable<WorkflowLog> BuildQuery(DevelopTurkeyContext context, WorkflowLogFilterDto filter)
    {
        var query = context.WorkflowLogs.AsQueryable();

        if (filter.InstitutionId.HasValue)
            query = query.Where(l => l.InstitutionId == filter.InstitutionId.Value);

        if (filter.RuleId.HasValue)
            query = query.Where(l => l.RuleId == filter.RuleId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TriggerEvent))
            query = query.Where(l => l.TriggerEvent == filter.TriggerEvent);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(l => l.Status == filter.Status);

        if (filter.TriggeredByUserId.HasValue)
            query = query.Where(l => l.TriggeredByUserId == filter.TriggeredByUserId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(l => l.ExecutedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(l => l.ExecutedAt <= filter.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var txt = filter.SearchText.ToLower();
            query = query.Where(l =>
                l.RuleName.ToLower().Contains(txt) ||
                l.TriggerEvent.ToLower().Contains(txt) ||
                (l.ErrorMessage != null && l.ErrorMessage.ToLower().Contains(txt)));
        }

        return query;
    }
}
