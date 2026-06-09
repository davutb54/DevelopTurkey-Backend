using Business.Abstract;
using Business.Models;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Concrete;

public class InstitutionFeatureManager : IInstitutionFeatureService
{
    private readonly IInstitutionFeatureValueDal _featureValueDal;
    private readonly IFeatureDefinitionDal _featureDefinitionDal;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogService _logService;
    private readonly IWorkflowEventBus _eventBus;
    private const string CacheKeyPrefix = "InstitutionFeatures_";

    public InstitutionFeatureManager(
        IInstitutionFeatureValueDal featureValueDal,
        IFeatureDefinitionDal featureDefinitionDal,
        IMemoryCache memoryCache,
        ILogService logService,
        IWorkflowEventBus eventBus)
    {
        _featureValueDal = featureValueDal;
        _featureDefinitionDal = featureDefinitionDal;
        _memoryCache = memoryCache;
        _logService = logService;
        _eventBus = eventBus;
    }

    private Dictionary<string, string> LoadFromDb(int institutionId)
    {
        // Kuruma özel değerleri getir
        var institutionValues = _featureValueDal.GetAll(v => v.InstitutionId == institutionId);
        
        // Tüm feature tanımlarını getir (default değerler için)
        var allDefinitions = _featureDefinitionDal.GetAll();
        
        // Önce tüm default değerleri yükle
        var result = allDefinitions.ToDictionary(d => d.Key, d => d.DefaultValue ?? "");
        
        // Kuruma özel değerlerle override et (Global-scope feature'lar override edilemez)
        foreach (var val in institutionValues)
        {
            var def = allDefinitions.FirstOrDefault(d => d.Id == val.FeatureDefinitionId);
            if (def != null && def.Scope != "Global")
            {
                result[def.Key] = val.Value;
            }
        }
        
        return result;
    }

    private Dictionary<string, string> GetCachedFeatures(int institutionId)
    {
        string cacheKey = $"{CacheKeyPrefix}{institutionId}";
        
        if (!_memoryCache.TryGetValue(cacheKey, out Dictionary<string, string> features))
        {
            features = LoadFromDb(institutionId);
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));
            _memoryCache.Set(cacheKey, features, cacheOptions);
        }
        
        return features ?? new Dictionary<string, string>();
    }

    public bool IsFeatureEnabled(int institutionId, string featureKey, bool defaultValue = false)
    {
        var features = GetCachedFeatures(institutionId);
        if (features.TryGetValue(featureKey, out var value))
        {
            if (bool.TryParse(value, out var boolVal)) return boolVal;
        }
        return defaultValue;
    }

    public string GetFeatureValue(int institutionId, string featureKey, string defaultValue = "")
    {
        var features = GetCachedFeatures(institutionId);
        return features.TryGetValue(featureKey, out var value) ? value : defaultValue;
    }

    public IDataResult<Dictionary<string, string>> GetAllForInstitution(int institutionId)
    {
        try
        {
            var features = GetCachedFeatures(institutionId);
            return new Core.Utilities.Results.SuccessDataResult<Dictionary<string, string>>(features);
        }
        catch (Exception ex)
        {
            return new Core.Utilities.Results.ErrorDataResult<Dictionary<string, string>>(null, ex.Message);
        }
    }

    public IResult SetFeatureValue(int institutionId, string featureKey, string value)
    {
        try
        {
            var definition = _featureDefinitionDal.Get(d => d.Key == featureKey);
            if (definition == null)
                return new Core.Utilities.Results.ErrorResult($"'{featureKey}' anahtarına sahip özellik tanımı bulunamadı.");

            var existing = _featureValueDal.Get(v => v.InstitutionId == institutionId && v.FeatureDefinitionId == definition.Id);
            
            if (existing != null)
            {
                string oldValue = existing.Value;
                existing.Value = value;
                _featureValueDal.Update(existing);

                _ = _eventBus.PublishAsync("institution.feature_changed", new RuleContext
                {
                    InstitutionId = institutionId,
                    OldValue = oldValue,
                    NewValue = value,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["FeatureKey"] = featureKey,
                        ["OldValue"] = oldValue,
                        ["NewValue"] = value
                    }
                });

                _logService.LogInfo("Feature", "Update",
                    $"Kurum {institutionId} için '{featureKey}' özelliği güncellendi.",
                    $"Eski Değer: {oldValue}, Yeni Değer: {value}",
                    institutionId);
            }
            else
            {
                string defaultVal = definition.DefaultValue ?? "";
                _featureValueDal.Add(new Entities.Concrete.InstitutionFeatureValue
                {
                    InstitutionId = institutionId,
                    FeatureDefinitionId = definition.Id,
                    Value = value
                });

                _ = _eventBus.PublishAsync("institution.feature_changed", new RuleContext
                {
                    InstitutionId = institutionId,
                    OldValue = defaultVal,
                    NewValue = value,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["FeatureKey"] = featureKey,
                        ["OldValue"] = defaultVal,
                        ["NewValue"] = value
                    }
                });

                _logService.LogInfo("Feature", "Add",
                    $"Kurum {institutionId} için '{featureKey}' özelliği ilk kez set edildi.",
                    $"Değer: {value}",
                    institutionId);
            }

            InvalidateCache(institutionId);
            return new Core.Utilities.Results.SuccessResult("Özellik değeri güncellendi.");
        }
        catch (Exception ex)
        {
            return new Core.Utilities.Results.ErrorResult(ex.Message);
        }
    }

    public IResult SetFeatureValues(int institutionId, Dictionary<string, string> values)
    {
        try
        {
            foreach (var kv in values)
            {
                var result = SetFeatureValue(institutionId, kv.Key, kv.Value);
                if (!result.Success) return result;
            }
            return new Core.Utilities.Results.SuccessResult("Tüm özellik değerleri güncellendi.");
        }
        catch (Exception ex)
        {
            return new Core.Utilities.Results.ErrorResult(ex.Message);
        }
    }

    public void InvalidateCache(int institutionId)
    {
        _memoryCache.Remove($"{CacheKeyPrefix}{institutionId}");
    }
}
