using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework;

public class EfLogDal : EfEntityRepositoryBase<Log, DevelopTurkeyContext>, ILogDal
{
    public List<Log> GetListByFilter(LogFilterDto filter)
    {
        using (var context = new DevelopTurkeyContext())
        {
            var query = context.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(l => l.Category == filter.Category);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(l => l.Action == filter.Action);

            if (!string.IsNullOrEmpty(filter.Level))
                query = query.Where(l => l.Level == filter.Level);

            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                query = query.Where(l =>
                    l.Message.Contains(filter.SearchText) ||
                    l.UserName.Contains(filter.SearchText) ||
                    l.IpAddress.Contains(filter.SearchText));
            }

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreationDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreationDate <= filter.EndDate.Value);

            return query.OrderByDescending(l => l.CreationDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();
        }
    }
}