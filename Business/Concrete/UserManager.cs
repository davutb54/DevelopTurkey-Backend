using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;

namespace Business.Concrete;

public class UserManager : IUserService
{
	private readonly IUserDal _userDal = new EfUserDal();
	private readonly ILogDal _logDal = new EfLogDal();
	public IDataResult<UserDetailDto?> GetById(int id)
	{
		var user = _userDal.GetUserDetail(u => u.Id == id);

		if (user != null)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserGetByIdOk + $" Id: {user.Id}",
				Type = "user,getById,OK"
			});
			return new SuccessDataResult<UserDetailDto?>(user);
		}

		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = Messages.UserNotFound,
			Type = "user,getById,Error"
		});
		return new ErrorDataResult<UserDetailDto?>(user, Messages.UserGetByIdError);
	}

	public IDataResult<List<UserDetailDto>> GetAll()
	{
		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = Messages.UserGetAllOk,
			Type = "user,getAll,OK"
		});
		return new SuccessDataResult<List<UserDetailDto>>(_userDal.GetUserDetails());
	}

	public IResult Login(UserForLoginDto userForLoginDto)
	{
		var user = _userDal.Get(u => u.UserName == userForLoginDto.UserName);

		if (user == null)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserLoginError + " " + Messages.UserNotFound,
				Type = "user,login,Error"
			});
			return new ErrorResult(Messages.UserNotFound);
		}

		if (user.Password != userForLoginDto.Password)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserLoginError + " " + Messages.UserPasswordError,
				Type = "user,login,Error"
			});
			return new ErrorResult(Messages.UserPasswordError);
		}

		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = Messages.UserLoginOk + $" Id: {user.Id} Username: {user.UserName}",
			Type = "user,login,OK"
		});
		return new SuccessDataResult<int>(user.Id,Messages.UserLoginOk);
	}

	public IResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto)
	{
		var user = _userDal.Get(u => u.Id == userForPasswordUpdateDto.Id);

		if (user == null)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserPasswordUpdateError + " " + Messages.UserNotFound,
				Type = "user,updatePassword,Error"
			});
			return new ErrorResult(Messages.UserNotFound);
		}

		if (user.Password != userForPasswordUpdateDto.OldPassword)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserPasswordUpdateError + $" Id: {user.Id} Username: {user.UserName} Hata Mesajı " + Messages.UserPasswordError,
				Type = "user,updatePassword,Error"
			});
			return new ErrorResult(Messages.UserPasswordError);
		}

		user.Password = userForPasswordUpdateDto.NewPassword;
		_userDal.Update(user);

		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = Messages.UserPasswordUpdateOk + $" Id: {user.Id} Username: {user.UserName}",
			Type = "user,updatePassword,OK"
		});
		return new SuccessResult(Messages.UserPasswordUpdateOk);
	}

	public IResult CheckUserExists(CheckExistsDto checkExistsDto)
	{
		var emailUser = _userDal.Get(u => u.Email == checkExistsDto.Email);
		var usernameUser = _userDal.Get(u => u.UserName == checkExistsDto.Username);

		if (emailUser != null)
		{
			return new SuccessResult(Messages.UserEmailIsFound);
		}

		if (usernameUser != null)
		{
			return new SuccessResult(Messages.UserUsernameIsFound);
		}

		return new ErrorResult(Messages.UserEmailAndUsernameIsNotFound);
	}

	public IResult Register(UserForRegisterDto userForRegisterDto)
	{
		User user = new User
		{
			UserName = userForRegisterDto.UserName,
			Name = userForRegisterDto.Name,
			Surname = userForRegisterDto.Surname,
			Email = userForRegisterDto.Email,
			CityCode = userForRegisterDto.CityCode,
			Gender = userForRegisterDto.GenderCode,
			Password = userForRegisterDto.Password,
			EmailNotificationPermission = userForRegisterDto.EmailNotificationPermission,
			RegisterDate = DateTime.Now
		};

		_userDal.Add(user);

		return new SuccessResult(Messages.UserRegisterOk);
	}

	public IResult UpdateUserDetails(UserForUpdateDto userForUpdateDto)
	{
		var user = _userDal.Get(u => u.Id == userForUpdateDto.Id);

		if (user == null)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserUpdateError + " " + Messages.UserNotFound,
				Type = "user,update,Error"
			});
			return new ErrorResult(Messages.UserNotFound);
		}

		user.Name = userForUpdateDto.Name;
		user.Surname = userForUpdateDto.Surname;
		user.Email = userForUpdateDto.Email;
		user.CityCode = userForUpdateDto.CityCode;
		user.Gender = userForUpdateDto.GenderCode;

		_userDal.Update(user);

		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = Messages.UserUpdateOk + $" Id: {user.Id} Username: {user.UserName}",
			Type = "user,update,OK"
		});

		return new SuccessResult(Messages.UserUpdateOk);
	}

	public IResult DeleteUser(int id)
	{
		var user = _userDal.Get(u => u.Id == id);

		if (user == null)
		{
			_logDal.Add(new Log
			{
				CreationDate = DateTime.Now,
				Message = Messages.UserDeleteError + " " + Messages.UserNotFound,
				Type = "user,delete,Error"
			});
			return new ErrorResult(Messages.UserNotFound);
		}

		user.IsDeleted = true;
		user.DeleteDate = DateTime.Now;
		_userDal.Update(user);

		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = Messages.UserDeleteOk + $" Id: {user.Id} Username: {user.UserName}",
			Type = "user,delete,OK"
		});

		return new SuccessResult(Messages.UserDeleteOk);
	}
}