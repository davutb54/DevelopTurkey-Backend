using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IFeatureDefinitionService
{
    IDataResult<FeatureDefinition> GetById(int id);
    IDataResult<List<FeatureDefinition>> GetAll();
    IDataResult<List<FeatureDefinition>> GetByGroupId(int groupId);
    IResult Add(FeatureDefinition featureDefinition);
    IResult Update(FeatureDefinition featureDefinition);
    IResult Delete(int id);
}
