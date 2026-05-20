using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using System.Linq.Expressions;

namespace DataAccess.Concrete.EntityFramework;

public class EfInstitutionDal : EfEntityRepositoryBase<Institution, DevelopTurkeyContext>, IInstitutionDal
{
	public List<InstitutionDetailDto> GetInstitutionDetails(Expression<Func<InstitutionDetailDto, bool>>? filter = null)
	{
		using DevelopTurkeyContext context = new DevelopTurkeyContext();

		var query = from i in context.Institutions
					select new InstitutionDetailDto
					{
						Id = i.Id,
						Name = i.Name,
						Subtitle = i.Subtitle,
						Domain = i.Domain,
						LogoUrl = i.LogoUrl,
						PrimaryColor = i.PrimaryColor,

						CustomFieldsJson = i.CustomFieldsJson,

						Status = i.Status
					};

		return filter == null ? query.ToList() : query.Where(filter).ToList();
	}
}
