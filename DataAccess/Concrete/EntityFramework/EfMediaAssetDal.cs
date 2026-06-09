using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfMediaAssetDal : EfEntityRepositoryBase<MediaAsset, DevelopTurkeyContext>, IMediaAssetDal
{
}
