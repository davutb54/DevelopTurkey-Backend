using System.Linq.Expressions;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using Core.Entities.Constants;
using DataAccess.Abstract;
using Entities.DTOs;
using Entities.DTOs.User;

namespace DataAccess.Concrete.EntityFramework;

public class EfUserDal : EfEntityRepositoryBase<User, DevelopTurkeyContext>, IUserDal
{
	public UserDetailDto? GetUserDetail(Expression<Func<UserDetailDto, bool>> filter)
	{
		using var context = new DevelopTurkeyContext();
		var result = from u in context.Users
					 where u.IsDeleted == false
					 select new UserDetailDto
					 {
						 Id = u.Id,
						 Name = u.Name,
						 Surname = u.Surname,
						 UserName = u.UserName,
						 Email = u.Email,
						 CityName = ConstantData.GetCity(u.CityCode).Text,
						 Gender = ConstantData.GetGender(u.Gender).Text,
						 EmailNotificationPermission = u.EmailNotificationPermission,
						 IsAdmin = u.IsAdmin,
						 IsExpert = u.IsExpert,
						 IsReported = u.IsReported,
						 IsDeleted = u.IsDeleted,
						 IsBanned = u.IsBanned,
                         ProfileImageUrl = u.ProfileImageUrl,
                         IsOfficial = u.IsOfficial,
                         IsEmailVerified = u.IsEmailVerified,
						 RegisterDate = u.RegisterDate,
						 DeleteDate = u.DeleteDate,
						 CityCode = u.CityCode,
						 GenderCode = u.Gender,
						 InstitutionId = u.InstitutionId
                     };
		return result.SingleOrDefault(filter);
	}

	public List<UserDetailDto> GetUserDetails(Expression<Func<UserDetailDto, bool>>? filter = null)
	{
		using var context = new DevelopTurkeyContext();
		var result = from u in context.Users
					 where u.IsDeleted == false
					 select new UserDetailDto
					 {
						 Id = u.Id,
						 Name = u.Name,
						 Surname = u.Surname,
						 UserName = u.UserName,
						 Email = u.Email,
						 CityName = ConstantData.GetCity(u.CityCode).Text,
						 Gender = ConstantData.GetGender(u.Gender).Text,
						 EmailNotificationPermission = u.EmailNotificationPermission,
						 IsAdmin = u.IsAdmin,
						 IsExpert = u.IsExpert,
                         ProfileImageUrl = u.ProfileImageUrl,
                         IsReported = u.IsReported,
						 IsDeleted = u.IsDeleted,
                         IsOfficial = u.IsOfficial,
                         IsBanned = u.IsBanned,
						 IsEmailVerified = u.IsEmailVerified,
						 RegisterDate = u.RegisterDate,
						 DeleteDate = u.DeleteDate,
						 InstitutionId = u.InstitutionId,
					 };
		return filter == null ? result.ToList() : result.Where(filter).ToList();
	}

    public (List<UserDetailDto> Items, int TotalCount) GetUserDetailsPaged(UserFilterDto filter)
    {
        using var context = new DevelopTurkeyContext();

        var query = from u in context.Users
                    where u.IsDeleted == false
                    select new UserDetailDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        UserName = u.UserName,
                        Email = u.Email,
                        CityName = Core.Entities.Constants.ConstantData.GetCity(u.CityCode).Text,
                        Gender = Core.Entities.Constants.ConstantData.GetGender(u.Gender).Text,
                        EmailNotificationPermission = u.EmailNotificationPermission,
                        IsAdmin = u.IsAdmin,
                        IsExpert = u.IsExpert,
                        ProfileImageUrl = u.ProfileImageUrl,
                        IsReported = u.IsReported,
                        IsDeleted = u.IsDeleted,
                        IsOfficial = u.IsOfficial,
                        IsBanned = u.IsBanned,
                        IsEmailVerified = u.IsEmailVerified,
                        RegisterDate = u.RegisterDate,
                        DeleteDate = u.DeleteDate,
                        InstitutionId = u.InstitutionId,
                        CityCode = u.CityCode,
                        GenderCode = u.Gender,
                    };

        // --- FİLTRELER ---
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = filter.SearchText.ToLower();
            query = query.Where(u =>
                u.UserName.ToLower().Contains(s) ||
                u.Name.ToLower().Contains(s) ||
                u.Surname.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(filter.RoleFilter))
        {
            query = filter.RoleFilter switch
            {
                "admin"    => query.Where(u => u.IsAdmin),
                "expert"   => query.Where(u => u.IsExpert),
                "official" => query.Where(u => u.IsOfficial),
                "banned"   => query.Where(u => u.IsBanned),
                _          => query
            };
        }

        if (!string.IsNullOrWhiteSpace(filter.EmailStatus))
        {
            query = filter.EmailStatus == "verified"
                ? query.Where(u => u.IsEmailVerified)
                : query.Where(u => !u.IsEmailVerified);
        }

        if (filter.InstitutionId.HasValue)
            query = query.Where(u => u.InstitutionId == filter.InstitutionId.Value);

        // --- SAYFALAMA ---
        int totalCount = query.Count();
        var items = query
            .OrderByDescending(u => u.RegisterDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return (items, totalCount);
    }
}