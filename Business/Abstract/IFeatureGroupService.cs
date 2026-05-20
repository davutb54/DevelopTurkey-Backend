using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IFeatureGroupService
{
    IDataResult<FeatureGroup> GetById(int id);
    IDataResult<List<FeatureGroup>> GetAll();
    IResult Add(FeatureGroup featureGroup);
    IResult Update(FeatureGroup featureGroup);
    IResult Delete(int id);
}
