using System.Linq.Expressions;
using Core.DataAccess;
using Core.Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;

namespace DataAccess.Abstract;

public interface IUserDal : IEntityRepository<User>
{
	UserDetailDto? GetUserDetail(Expression<Func<UserDetailDto,bool>> filter);
	List<UserDetailDto> GetUserDetails(Expression<Func<UserDetailDto, bool>>? filter = null);
    (List<UserDetailDto> Items, int TotalCount) GetUserDetailsPaged(UserFilterDto filter);
}