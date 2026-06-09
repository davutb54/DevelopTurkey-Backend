using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class MediaAssetManager : IMediaAssetService
{
    private readonly IMediaAssetDal _mediaAssetDal;

    public MediaAssetManager(IMediaAssetDal mediaAssetDal)
    {
        _mediaAssetDal = mediaAssetDal;
    }

    public IResult Add(MediaAsset asset)
    {
        asset.CreatedAt = DateTime.Now;
        asset.IsDeleted = false;
        _mediaAssetDal.Add(asset);
        return new SuccessResult("Medya varlığı eklendi.");
    }

    public IResult SoftDelete(int id)
    {
        var asset = _mediaAssetDal.Get(a => a.Id == id);
        if (asset == null) return new ErrorResult("Medya varlığı bulunamadı.");
        asset.IsDeleted = true;
        asset.DeletedAt = DateTime.Now;
        _mediaAssetDal.Update(asset);
        return new SuccessResult("Medya varlığı silindi.");
    }

    public IDataResult<List<MediaAsset>> GetByOwner(string ownerType, int ownerId)
    {
        var assets = _mediaAssetDal.GetAll(a =>
            a.OwnerType == ownerType && a.OwnerId == ownerId && !a.IsDeleted);
        return new SuccessDataResult<List<MediaAsset>>(assets);
    }

    public IDataResult<List<MediaAsset>> GetByOwners(string ownerType, List<int> ownerIds)
    {
        var assets = _mediaAssetDal.GetAll(a =>
            a.OwnerType == ownerType && ownerIds.Contains(a.OwnerId) && !a.IsDeleted);
        return new SuccessDataResult<List<MediaAsset>>(assets);
    }
}
