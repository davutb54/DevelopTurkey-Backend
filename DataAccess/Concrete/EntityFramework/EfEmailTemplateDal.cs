using Core.DataAccess.EntityFramework;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using DataAccess.Abstract;

namespace DataAccess.Concrete.EntityFramework;

public class EfEmailTemplateDal : EfEntityRepositoryBase<EmailTemplate, DevelopTurkeyContext>, IEmailTemplateDal
{
}
