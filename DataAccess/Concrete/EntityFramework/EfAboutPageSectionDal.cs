using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework;

public class EfAboutPageSectionDal : EfEntityRepositoryBase<AboutPageSection, DevelopTurkeyContext>, IAboutPageSectionDal
{
}
