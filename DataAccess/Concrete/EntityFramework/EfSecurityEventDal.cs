using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework;

public class EfSecurityEventDal : EfEntityRepositoryBase<SecurityEvent, DevelopTurkeyContext>, ISecurityEventDal
{
    public (List<SecurityEvent> Items, int TotalCount) GetPaged(SecurityEventFilterDto filter)
    {
        using var context = new DevelopTurkeyContext();

        var query = context.SecurityEvents.AsQueryable();

        if (!string.IsNullOrEmpty(filter.IpAddress))
            query = query.Where(e => e.IpAddress.Contains(filter.IpAddress));

        if (filter.UserId.HasValue)
            query = query.Where(e => e.UserId == filter.UserId.Value);

        if (!string.IsNullOrEmpty(filter.EventType))
            query = query.Where(e => e.EventType == filter.EventType);

        if (!string.IsNullOrEmpty(filter.Severity))
            query = query.Where(e => e.Severity == filter.Severity);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CreatedAt <= filter.EndDate.Value);

        var total = query.Count();
        var items = query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return (items, total);
    }
}
