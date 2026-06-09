using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IMediaAssetService
{
    IResult Add(MediaAsset asset);
    IResult SoftDelete(int id);
    IDataResult<List<MediaAsset>> GetByOwner(string ownerType, int ownerId);
    IDataResult<List<MediaAsset>> GetByOwners(string ownerType, List<int> ownerIds);
}
