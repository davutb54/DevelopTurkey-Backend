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
    private readonly ILogService _logService;
    private readonly ITokenHelper _tokenHelper;
    private readonly IInstitutionService _institutionService;

    public UserManager(IUserDal userDal, ILogService logService, ITokenHelper tokenHelper, IInstitutionService institutionService)
    {
        _userDal = userDal;
        _logService = logService;
        _tokenHelper = tokenHelper;
        _institutionService = institutionService;
    }

    public IDataResult<UserDetailDto?> GetById(int id)
    {
        var user = _userDal.GetUserDetail(u => u.Id == id);

        if (user != null)
        {
            return new SuccessDataResult<UserDetailDto?>(user, Messages.UserGetByIdOk);
        }

        return new ErrorDataResult<UserDetailDto?>(user, Messages.UserGetByIdError);
    }

    public IDataResult<List<UserDetailDto>> GetAll()
    {
        return new SuccessDataResult<List<UserDetailDto>>(_userDal.GetUserDetails(), Messages.UserGetAllOk);
    }
    public IResult Login(UserForLoginDto userForLoginDto)
    {
        var user = _userDal.Get(u => u.UserName == userForLoginDto.UserName);

        if (user == null || user.IsDeleted)
        {
            _logService.LogWarning("Auth", "Login", $"Kullanıcı bulunamadı: {userForLoginDto.UserName}");
            return new ErrorResult(Messages.UserNotFound);
        }

        if (user.IsBanned)
        {
            _logService.LogWarning("Security", "Login", $"Banlı kullanıcı giriş denemesi: {user.UserName}");
            return new ErrorResult("Hesabınız kuralları ihlal ettiğiniz gerekçesiyle sistem yöneticileri tarafından askıya alınmıştır.");
        }

        if (user.InstitutionId != 1)
        {
            var institutionResult = _institutionService.GetById(user.InstitutionId);
            if (institutionResult.Success && institutionResult.Data != null && !institutionResult.Data.Status)
            {
                _logService.LogWarning("Auth", "Login", $"Devre dışı kurumdan giriş denemesi: {user.UserName} ({institutionResult.Data.Name})");

                return new ErrorResult($"Bağlı bulunduğunuz '{institutionResult.Data.Name}' ağı sistem yöneticileri tarafından devre dışı bırakılmıştır. Lütfen daha sonra tekrar deneyiniz.");
            }
        }

        if (!HashingHelper.VerifyPasswordHash(userForLoginDto.Password, user.PasswordHash, user.PasswordSalt))
        {
            _logService.LogWarning("Auth", "Login", $"Hatalı şifre girişi: {user.UserName}");
            return new ErrorResult(Messages.UserPasswordError);
        }

        _logService.LogInfo("Auth", "Login", $"Başarılı giriş - ID: {user.Id}, Kullanıcı: {user.UserName}");
        return new SuccessResult(Messages.UserLoginOk);
    }

    public IDataResult<AccessToken> CreateAccessToken(User user)
    {
        if (_tokenHelper == null) return new ErrorDataResult<AccessToken>(null, "Token servisi yapılandırılmadı.");

        var accessToken = _tokenHelper.CreateToken(user);
        accessToken.UserId = user.Id;
        return new SuccessDataResult<AccessToken>(accessToken, "Token oluşturuldu");
    }

    public IResult Register(UserForRegisterDto userForRegisterDto)
    {
        byte[] passwordHash, passwordSalt;
        HashingHelper.CreatePasswordHash(userForRegisterDto.Password, out passwordHash, out passwordSalt);

        string emailDomain = userForRegisterDto.Email.Split('@')[1].ToLower();

        var institutionResult = _institutionService.GetByDomain(emailDomain);

        int assignedInstitutionId = (institutionResult.Success && institutionResult.Data != null)
            ? institutionResult.Data.Id
            : 1;

        if (institutionResult.Success && institutionResult.Data != null)
        {
            if (!institutionResult.Data.Status)
            {
                return new ErrorResult($"'{institutionResult.Data.Name}' ağı yöneticiler tarafından geçici olarak devre dışı bırakılmıştır. Bu kuruma ait e-posta adresinizle şu an kayıt oluşturamazsınız.");
            }

            assignedInstitutionId = institutionResult.Data.Id;
        }

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
            IsEmailVerified = false,
            InstitutionId = assignedInstitutionId
        };

        _userDal.Add(user);

        _logService.LogInfo("Auth", "Register", $"Yeni kullanıcı kaydı - ID: {user.Id}, Kullanıcı: {user.UserName}");

        return new SuccessResult(Messages.UserRegisterOk);
    }

    public IResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto)
    {
        var user = _userDal.Get(u => u.Id == userForPasswordUpdateDto.Id);

        if (user == null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }

        if (!HashingHelper.VerifyPasswordHash(userForPasswordUpdateDto.OldPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logService.LogWarning("Security", "UpdatePassword", $"Hatalı eski şifre girişi - ID: {user.Id}, Kullanıcı: {user.UserName}");
            return new ErrorResult(Messages.UserPasswordError);
        }

        byte[] newHash, newSalt;
        HashingHelper.CreatePasswordHash(userForPasswordUpdateDto.NewPassword, out newHash, out newSalt);

        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;

        _userDal.Update(user);

        _logService.LogInfo("Security", "UpdatePassword", $"Şifre güncellendi - ID: {user.Id}, Kullanıcı: {user.UserName}");
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
            return new ErrorResult(Messages.UserNotFound);
        }

        user.Name = userForUpdateDto.Name;
        user.Surname = userForUpdateDto.Surname;
        user.Email = userForUpdateDto.Email;
        user.CityCode = userForUpdateDto.CityCode;
        user.Gender = userForUpdateDto.GenderCode;

        _userDal.Update(user);

        _logService.LogInfo("Auth", "UpdateDetails", $"Kullanıcı bilgileri güncellendi - ID: {user.Id}, Kullanıcı: {user.UserName}");

        return new SuccessResult(Messages.UserUpdateOk);
    }

    public IResult DeleteUser(int id)
    {
        var user = _userDal.Get(u => u.Id == id);

        if (user == null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }

        user.IsDeleted = true;
        user.DeleteDate = DateTime.Now;
        _userDal.Update(user);

        _logService.LogWarning("Security", "Delete", $"Kullanıcı silindi - ID: {user.Id}, Kullanıcı: {user.UserName}");

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

        _logService.LogInfo("Security", "ResetPassword", $"Kullanıcı şifresi sıfırlandı - ID: {user.Id}");

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

        _logService.LogWarning("Moderation", "Ban", $"Kullanıcı yasaklandı - ID: {user.Id}, Kullanıcı: {user.UserName}");

        return new SuccessResult($"Kullanıcı (ID: {user.Id}) yasaklandı.");
    }

    public IResult UnbanUser(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        user.IsBanned = false;
        _userDal.Update(user);

        _logService.LogInfo("Moderation", "Unban", $"Kullanıcı yasağı kaldırıldı - ID: {user.Id}, Kullanıcı: {user.UserName}");

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
        _logService.LogInfo("Moderation", "Report", $"Kullanıcı raporlandı - ID: {user.Id}");
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
        _logService.LogWarning("Security", "ToggleAdminRole", $"Admin rolü {(user.IsAdmin ? "verildi" : "kaldırıldı")} - ID: {user.Id}, Kullanıcı: {user.UserName}");
        _userDal.Update(user);
        string action = user.IsAdmin ? "Admin rolü verildi" : "Admin rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }

    public IResult ToggleExpertRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsExpert = !user.IsExpert;
        _logService.LogWarning("Security", "ToggleExpertRole", $"Uzman rolü {(user.IsExpert ? "verildi" : "kaldırıldı")} - ID: {user.Id}, Kullanıcı: {user.UserName}");
        _userDal.Update(user);
        string action = user.IsExpert ? "Uzman rolü verildi" : "Uzman rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }

    public IResult ToggleOfficialRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsOfficial = !user.IsOfficial;
        _logService.LogWarning("Security", "ToggleOfficialRole", $"Resmi rolü {(user.IsOfficial ? "verildi" : "kaldırıldı")} - ID: {user.Id}, Kullanıcı: {user.UserName}");
        _userDal.Update(user);
        string action = user.IsOfficial ? "Resmi rolü verildi" : "Resmi rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }
}