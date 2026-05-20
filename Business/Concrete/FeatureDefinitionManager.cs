using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class FeatureDefinitionManager : IFeatureDefinitionService
{
    private readonly IFeatureDefinitionDal _featureDefinitionDal;
    private readonly ILogService _logService;

    public FeatureDefinitionManager(IFeatureDefinitionDal featureDefinitionDal, ILogService logService)
    {
        _featureDefinitionDal = featureDefinitionDal;
        _logService = logService;
    }

    public IDataResult<FeatureDefinition> GetById(int id)
    {
        var result = _featureDefinitionDal.Get(f => f.Id == id);
        if (result == null) return new ErrorDataResult<FeatureDefinition>(default, "Özellik tanımı bulunamadı.");
        return new SuccessDataResult<FeatureDefinition>(result);
    }

    public IDataResult<List<FeatureDefinition>> GetAll()
    {
        return new SuccessDataResult<List<FeatureDefinition>>(_featureDefinitionDal.GetAll());
    }

    public IDataResult<List<FeatureDefinition>> GetByGroupId(int groupId)
    {
        return new SuccessDataResult<List<FeatureDefinition>>(_featureDefinitionDal.GetAll(f => f.GroupId == groupId));
    }

    public IResult Add(FeatureDefinition featureDefinition)
    {
        _featureDefinitionDal.Add(featureDefinition);
        _logService.LogInfo("FeatureDefinition", "Add", 
            $"Yeni özellik tanımı eklendi: '{featureDefinition.DisplayName}' ({featureDefinition.Key})", 
            $"Default Value: {featureDefinition.DefaultValue}");
        return new SuccessResult("Özellik tanımı eklendi.");
    }

    public IResult Update(FeatureDefinition featureDefinition)
    {
        _featureDefinitionDal.Update(featureDefinition);
        _logService.LogInfo("FeatureDefinition", "Update", 
            $"Özellik tanımı güncellendi: '{featureDefinition.DisplayName}' ({featureDefinition.Key})", 
            $"Default Value: {featureDefinition.DefaultValue}");
        return new SuccessResult("Özellik tanımı güncellendi.");
    }

    public IResult Delete(int id)
    {
        var result = _featureDefinitionDal.Get(f => f.Id == id);
        if (result == null) return new ErrorResult("Özellik tanımı bulunamadı.");
        _featureDefinitionDal.Delete(result);
        _logService.LogInfo("FeatureDefinition", "Delete", 
            $"Özellik tanımı silindi: '{result.DisplayName}' ({result.Key})");
        return new SuccessResult("Özellik tanımı silindi.");
    }
}
