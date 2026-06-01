using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract;

public interface IAnnouncementDal : IEntityRepository<Announcement>
{
}
