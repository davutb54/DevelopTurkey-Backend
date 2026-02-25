using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;

namespace Business.Concrete;

public class UserManager : IUserService
{
    private readonly IUserDal _userDal;
    private readonly ILogDal _logDal;
    private readonly ITokenHelper _tokenHelper;

    public UserManager(IUserDal userDal, ILogDal logDal, ITokenHelper tokenHelper)
    {
        _userDal = userDal;
        _logDal = logDal;
        _tokenHelper = tokenHelper;
    }

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
            return new SuccessDataResult<UserDetailDto?>(user, Messages.UserGetByIdOk);
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
        return new SuccessDataResult<List<UserDetailDto>>(_userDal.GetUserDetails(), Messages.UserGetAllOk);
    }
    public IResult Login(UserForLoginDto userForLoginDto)
    {
        var user = _userDal.Get(u => u.UserName == userForLoginDto.UserName);

        if (user == null || user.IsDeleted)
        {
            _logDal.Add(new Log
            {
                CreationDate = DateTime.Now,
                Message = Messages.UserLoginError + " " + Messages.UserNotFound,
                Type = "user,login,Error"
            });
            return new ErrorResult(Messages.UserNotFound);
        }

        if (user.IsBanned)
        {
            _logDal.Add(new Log
            {
                CreationDate = DateTime.Now,
                Message = $"Banlı Giriş Denemesi: {user.UserName}",
                Type = "user,login,Banned"
            });
            return new ErrorResult("Hesabınız kuralları ihlal ettiğiniz gerekçesiyle sistem yöneticileri tarafından askıya alınmıştır.");
        }

        if (!HashingHelper.VerifyPasswordHash(userForLoginDto.Password, user.PasswordHash, user.PasswordSalt))
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
        return new SuccessResult(Messages.UserLoginOk);
    }

    public IDataResult<AccessToken> CreateAccessToken(User user)
    {
        if (_tokenHelper == null) return new ErrorDataResult<AccessToken>(null, "Token servisi yapılandırılmadı.");

        var accessToken = _tokenHelper.CreateToken(user);
        return new SuccessDataResult<AccessToken>(accessToken, "Token oluşturuldu");
    }

    public IResult Register(UserForRegisterDto userForRegisterDto)
    {
        byte[] passwordHash, passwordSalt;
        HashingHelper.CreatePasswordHash(userForRegisterDto.Password, out passwordHash, out passwordSalt);

        User user = new User
        {
            UserName = userForRegisterDto.UserName,
            Name = userForRegisterDto.Name,
            Surname = userForRegisterDto.Surname,
            Email = userForRegisterDto.Email,
            CityCode = userForRegisterDto.CityCode,
            Gender = userForRegisterDto.GenderCode,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            EmailNotificationPermission = userForRegisterDto.EmailNotificationPermission,
            RegisterDate = DateTime.Now,
            IsAdmin = false,
            IsExpert = false,
            IsDeleted = false,
            IsReported = false,
            IsBanned = false,
            IsEmailVerified = false
        };

        _userDal.Add(user);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.UserRegisterOk + $" Id: {user.Id} Username: {user.UserName}",
            Type = "user,register,OK"
        });

        return new SuccessResult(Messages.UserRegisterOk);
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

        if (!HashingHelper.VerifyPasswordHash(userForPasswordUpdateDto.OldPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logDal.Add(new Log
            {
                CreationDate = DateTime.Now,
                Message = Messages.UserPasswordUpdateError + $" Id: {user.Id} Username: {user.UserName} Hata Mesajı " + Messages.UserPasswordError,
                Type = "user,updatePassword,Error"
            });
            return new ErrorResult(Messages.UserPasswordError);
        }

        byte[] newHash, newSalt;
        HashingHelper.CreatePasswordHash(userForPasswordUpdateDto.NewPassword, out newHash, out newSalt);

        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;

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

    public User GetByUserName(string userName)
    {
        return _userDal.Get(u => u.UserName == userName);
    }

    public IResult Update(User user)
    {
        _userDal.Update(user);
        return new SuccessResult(Messages.UserUpdateOk);
    }

    public IResult ResetPassword(int userId, string newPassword)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        byte[] passwordHash, passwordSalt;
        HashingHelper.CreatePasswordHash(newPassword, out passwordHash, out passwordSalt);

        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        _userDal.Update(user);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) şifresi sıfırlandı.",
            Type = "user,resetPassword,OK"
        });

        return new SuccessResult("Şifreniz başarıyla sıfırlandı.");
    }

    public User GetByEmail(string email)
    {
        return _userDal.Get(u => u.Email == email);
    }

    public IResult BanUser(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        user.IsBanned = true;
        _userDal.Update(user);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) yasaklandı.",
            Type = "user,ban,OK"
        });

        return new SuccessResult($"Kullanıcı (ID: {user.Id}) yasaklandı.");
    }

    public IResult UnbanUser(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        user.IsBanned = false;
        _userDal.Update(user);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) yasağı kaldırıldı.",
            Type = "user,unban,OK"
        });

        return new SuccessResult($"Kullanıcı (ID: {user.Id}) yasağı kaldırıldı.");
    }

    public int GetUserCount()
    {
        return _userDal.Count();
    }

    public int GetBannedUserCount()
    {
        return _userDal.Count(u => u.IsBanned == true);
    }

    public IResult ReportUser(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsReported = true;
        _userDal.Update(user);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) raporlandı.",
            Type = "user,report,OK"
        });
        return new SuccessResult($"Kullanıcı (ID: {user.Id}) raporlandı");
    }

    public IResult UnReportUser(int id)
    {
        var user = _userDal.Get(u => u.Id == id);
        if (user != null)
        {
            user.IsReported = false;
            _userDal.Update(user);
        }
        return new SuccessResult();
    }

    public IResult ToggleAdminRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsAdmin = !user.IsAdmin;
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) admin rolü {(user.IsAdmin ? "verildi" : "kaldırıldı")}.",
            Type = "user,toggleAdminRole,OK"
        });
        _userDal.Update(user);
        string action = user.IsAdmin ? "Admin rolü verildi" : "Admin rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }

    public IResult ToggleExpertRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsExpert = !user.IsExpert;
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) uzman rolü {(user.IsExpert ? "verildi" : "kaldırıldı")}.",
            Type = "user,toggleExpertRole,OK"
        });
        _userDal.Update(user);
        string action = user.IsExpert ? "Uzman rolü verildi" : "Uzman rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }

    public IResult ToggleOfficialRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsOfficial = !user.IsOfficial;
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı (ID: {user.Id}) resmi rolü {(user.IsOfficial ? "verildi" : "kaldırıldı")}.",
            Type = "user,toggleOfficialRole,OK"
        });
        _userDal.Update(user);
        string action = user.IsOfficial ? "Resmi rolü verildi" : "Resmi rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }
}