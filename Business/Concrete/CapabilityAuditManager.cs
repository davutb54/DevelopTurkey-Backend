using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs.Capability;

namespace Business.Concrete;

public class CapabilityAuditManager : ICapabilityAuditService
{
    private readonly ICapabilityAuditLogDal _auditDal;

    public CapabilityAuditManager(ICapabilityAuditLogDal auditDal)
    {
        _auditDal = auditDal;
    }

    public IResult Add(CapabilityAuditLog log)
    {
        log.CreatedAt = DateTime.UtcNow;
        _auditDal.Add(log);
        return new SuccessResult(Messages.CapabilityAuditAdded);
    }

    public IDataResult<List<CapabilityAuditLog>> GetByFilter(CapabilityAuditFilterDto filter)
    {
        var query = _auditDal.GetAll().AsQueryable();

        if (filter.ActorUserId.HasValue)
            query = query.Where(a => a.ActorUserId == filter.ActorUserId.Value);

        if (filter.TargetUserId.HasValue)
            query = query.Where(a => a.TargetUserId == filter.TargetUserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (filter.From.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.To.Value);

        var result = query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new SuccessDataResult<List<CapabilityAuditLog>>(result);
    }
}
