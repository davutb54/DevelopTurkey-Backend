using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class FeatureGroupManager : IFeatureGroupService
{
    private readonly IFeatureGroupDal _featureGroupDal;

    public FeatureGroupManager(IFeatureGroupDal featureGroupDal)
    {
        _featureGroupDal = featureGroupDal;
    }

    public IDataResult<FeatureGroup> GetById(int id)
    {
        var result = _featureGroupDal.Get(f => f.Id == id);
        if (result == null) return new ErrorDataResult<FeatureGroup>(default, "Özellik grubu bulunamadı.");
        return new SuccessDataResult<FeatureGroup>(result);
    }

    public IDataResult<List<FeatureGroup>> GetAll()
    {
        return new SuccessDataResult<List<FeatureGroup>>(_featureGroupDal.GetAll());
    }

    public IResult Add(FeatureGroup featureGroup)
    {
        _featureGroupDal.Add(featureGroup);
        return new SuccessResult("Özellik grubu eklendi.");
    }

    public IResult Update(FeatureGroup featureGroup)
    {
        _featureGroupDal.Update(featureGroup);
        return new SuccessResult("Özellik grubu güncellendi.");
    }

    public IResult Delete(int id)
    {
        var result = _featureGroupDal.Get(f => f.Id == id);
        if (result == null) return new ErrorResult("Özellik grubu bulunamadı.");
        _featureGroupDal.Delete(result);
        return new SuccessResult("Özellik grubu silindi.");
    }
}
