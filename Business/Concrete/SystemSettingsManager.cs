using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Concrete;

public class SystemSettingsManager : ISystemSettingsService
{
    private const string CacheKey = "system_settings";

    private readonly ISystemSettingsDal _systemSettingsDal;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogService _logService;

    public SystemSettingsManager(
        ISystemSettingsDal systemSettingsDal,
        IMemoryCache memoryCache,
        ILogService logService)
    {
        _systemSettingsDal = systemSettingsDal;
        _memoryCache = memoryCache;
        _logService = logService;
    }

    public IDataResult<SystemSettings> Get()
    {
        if (_memoryCache.TryGetValue(CacheKey, out SystemSettings? cached) && cached != null)
        {
            return new SuccessDataResult<SystemSettings>(cached);
        }

        var settings = _systemSettingsDal.GetAll().FirstOrDefault();

        if (settings == null)
        {
            settings = new SystemSettings
            {
                IsMaintenanceMode = false,
                DisableNewRegistrations = false,
                MaintenanceMessage = null,
                LastUpdatedAt = DateTime.Now,
                UpdatedByUserId = null
            };
            _systemSettingsDal.Add(settings);
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        _memoryCache.Set(CacheKey, settings, cacheOptions);

        return new SuccessDataResult<SystemSettings>(settings);
    }

    public IResult Update(SystemSettings settings, int adminUserId)
    {
        settings.LastUpdatedAt = DateTime.Now;
        settings.UpdatedByUserId = adminUserId;

        _systemSettingsDal.Update(settings);

        _memoryCache.Remove(CacheKey);

        _logService.LogInfo(
            "AdminAction",
            "SystemSettingsUpdate",
            $"Sistem ayarları güncellendi. AdminUserId: {adminUserId}",
            $"MaintenanceMode: {settings.IsMaintenanceMode}, DisableRegistrations: {settings.DisableNewRegistrations}");

        return new SuccessResult("Sistem ayarları başarıyla güncellendi.");
    }
}
