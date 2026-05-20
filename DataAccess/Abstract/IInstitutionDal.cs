using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;
using System.Linq.Expressions;

namespace DataAccess.Abstract;

public interface IInstitutionDal : IEntityRepository<Institution>
{
	List<InstitutionDetailDto> GetInstitutionDetails(Expression<Func<InstitutionDetailDto, bool>>? filter = null);
}
