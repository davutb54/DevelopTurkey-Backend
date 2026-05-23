using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.DTOs.Capability;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Concrete;

public class CapabilityManager : ICapabilityService
{
    private readonly ICapabilityDal _capabilityDal;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "capabilities:all";

    public CapabilityManager(ICapabilityDal capabilityDal, IMemoryCache cache)
    {
        _capabilityDal = capabilityDal;
        _cache = cache;
    }

    public IDataResult<List<CapabilityDto>> GetAll()
    {
        if (_cache.TryGetValue(CacheKey, out List<CapabilityDto>? cached) && cached != null)
            return new SuccessDataResult<List<CapabilityDto>>(cached);

        var list = _capabilityDal.GetAll(c => c.IsActive)
            .Select(ToDto)
            .ToList();

        _cache.Set(CacheKey, list, TimeSpan.FromMinutes(5));
        return new SuccessDataResult<List<CapabilityDto>>(list);
    }

    public IDataResult<CapabilityDto> GetByCode(string code)
    {
        var cap = _capabilityDal.Get(c => c.Code == code && c.IsActive);
        if (cap == null) return new ErrorDataResult<CapabilityDto>(default!, Messages.CapabilityNotFound);
        return new SuccessDataResult<CapabilityDto>(ToDto(cap));
    }

    public IDataResult<List<CapabilityDto>> GetByCategory(string category)
    {
        var list = _capabilityDal.GetAll(c => c.Category == category && c.IsActive)
            .Select(ToDto)
            .ToList();
        return new SuccessDataResult<List<CapabilityDto>>(list);
    }

    private static CapabilityDto ToDto(Entities.Concrete.Capability c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Description = c.Description,
        Category = c.Category,
        IsSystem = c.IsSystem,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
    };
}
