using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;

namespace Business.Abstract;

public interface IUserService
{
	IDataResult<UserDetailDto?> GetById(int id);
	IResult Login(UserForLoginDto userForLoginDto);
	IDataResult<List<UserDetailDto>> GetAll();
	IResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto);
	IResult CheckUserExists(CheckExistsDto checkExistsDto);
	IResult Register(UserForRegisterDto userForRegisterDto);
	IResult UpdateUserDetails(UserForUpdateDto userForUpdateDto);
	IResult DeleteUser(int id);
}