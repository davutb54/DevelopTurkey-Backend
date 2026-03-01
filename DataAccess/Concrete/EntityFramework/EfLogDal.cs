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

            if (!string.IsNullOrEmpty(filter.Type))
            {
                query = query.Where(l => l.Type.Contains(filter.Type));
            }

            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                query = query.Where(l => l.Message.Contains(filter.SearchText));
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(l => l.CreationDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(l => l.CreationDate <= filter.EndDate.Value);
            }

            return query.OrderByDescending(l => l.CreationDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();
        }
    }
}