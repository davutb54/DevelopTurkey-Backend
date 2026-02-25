using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.JWT;
using Entities.DTOs;
using Entities.DTOs.User;

namespace Business.Abstract;

public interface IUserService
{
    IDataResult<UserDetailDto?> GetById(int id);
    IDataResult<List<UserDetailDto>> GetAll();
    IResult Login(UserForLoginDto userForLoginDto);
    IResult Register(UserForRegisterDto userForRegisterDto);
    IDataResult<AccessToken> CreateAccessToken(User user);
    IResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto);
    IResult CheckUserExists(CheckExistsDto checkExistsDto);
    IResult UpdateUserDetails(UserForUpdateDto userForUpdateDto);
    User GetByUserName(string userName);
    IResult DeleteUser(int id);
    IResult Update(User user);
    IResult ResetPassword(int userId, string newPassword);
    User GetByEmail(string email);
    IResult BanUser(int userId);
    IResult UnbanUser(int userId);
    int GetUserCount();
    int GetBannedUserCount();
    IResult ReportUser(int userId);
    IResult UnReportUser(int userId);
    IResult ToggleAdminRole(int userId);
    IResult ToggleExpertRole(int userId);
    IResult ToggleOfficialRole(int userId);
}