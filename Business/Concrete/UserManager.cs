using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.JWT;
using Core.Utilities.Context;
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
    private readonly IClientContext _clientContext;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly INotificationService _notificationService;

    public UserManager(IUserDal userDal, ILogService logService, ITokenHelper tokenHelper, IInstitutionService institutionService, IClientContext clientContext, ISystemSettingsService systemSettingsService, INotificationService notificationService)
    {
        _userDal = userDal;
        _logService = logService;
        _tokenHelper = tokenHelper;
        _institutionService = institutionService;
        _clientContext = clientContext;
        _systemSettingsService = systemSettingsService;
        _notificationService = notificationService;
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

    public IDataResult<UserPublicProfileDto?> GetPublicProfile(int id)
    {
        var user = _userDal.GetUserDetail(u => u.Id == id);

        if (user != null)
        {
            var publicProfile = new UserPublicProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname,
                CityName = user.CityName,
                Gender = user.Gender,
                IsAdmin = user.IsAdmin,
                IsExpert = user.IsExpert,
                IsOfficial = user.IsOfficial,
                RegisterDate = user.RegisterDate,
                ProfileImageUrl = user.ProfileImageUrl,
                InstitutionId = user.InstitutionId
            };
            return new SuccessDataResult<UserPublicProfileDto?>(publicProfile, Messages.UserGetByIdOk);
        }

        return new ErrorDataResult<UserPublicProfileDto?>(null, Messages.UserGetByIdError);
    }

    public IDataResult<List<UserDetailDto>> GetAll()
    {
        return new SuccessDataResult<List<UserDetailDto>>(_userDal.GetUserDetails(), Messages.UserGetAllOk);
    }

    public IDataResult<(List<UserDetailDto> Items, int TotalCount)> GetAllPaged(UserFilterDto filter)
    {
        var result = _userDal.GetUserDetailsPaged(filter);
        return new SuccessDataResult<(List<UserDetailDto> Items, int TotalCount)>(result, Messages.UserGetAllOk);
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

    public IDataResult<AccessToken> CreateAccessToken(User user, int? impersonatedById = null)
    {
        if (_tokenHelper == null) return new ErrorDataResult<AccessToken>(null, "Token servisi yapılandırılmadı.");

        var accessToken = _tokenHelper.CreateToken(user, impersonatedById);
        accessToken.UserId = user.Id;
        return new SuccessDataResult<AccessToken>(accessToken, "Token oluşturuldu");
    }

    public bool VerifyPassword(int userId, string password)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return false;
        return HashingHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);
    }

    public IResult Register(UserForRegisterDto userForRegisterDto)
    {
        var systemSettings = _systemSettingsService.Get();
        if (systemSettings.Success && (systemSettings.Data.DisableNewRegistrations || systemSettings.Data.IsMaintenanceMode))
        {
            string message = systemSettings.Data.IsMaintenanceMode 
                ? "Sistem şu anda bakım aşamasında olduğu için yeni üye kaydı yapılamamaktadır." 
                : "Sistem yöneticileri yeni üye alımını geçici olarak durdurmuştur. Lütfen daha sonra tekrar deneyiniz.";
            return new ErrorResult(message);
        }

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
        userForPasswordUpdateDto.Id = _clientContext.GetUserId() ?? 0;
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
        userForUpdateDto.Id = _clientContext.GetUserId() ?? 0;
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

        var currentUserId = _clientContext.GetUserId();

        if (currentUserId.HasValue && currentUserId.Value != id) {
            _logService.LogWarning("AdminAction", "Delete", $"Kullanıcı hesabı GÖREVLİ tarafından silindi - ID: {user.Id}, Kullanıcı: {user.UserName}");
        } else {
            _logService.LogWarning("Security", "Delete", $"Kullanıcı kendi hesabını kalıcı olarak sildi - ID: {user.Id}, Kullanıcı: {user.UserName}");
        }
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

        _logService.LogWarning("AdminAction", "Ban", $"Kullanıcı yasaklandı - ID: {user.Id}, Kullanıcı: {user.UserName}");

        try
        {
            _notificationService.Add(new Notification
            {
                UserId = userId,
                Title = "Hesabınız askıya alındı",
                Message = "Hesabınız platform kurallarını ihlal ettiği için yöneticiler tarafından askıya alınmıştır.",
                Type = "AdminWarning",
                ReferenceLink = null
            });
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }

        return new SuccessResult($"Kullanıcı (ID: {user.Id}) yasaklandı.");
    }

    public IResult UnbanUser(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        user.IsBanned = false;
        _userDal.Update(user);

        _logService.LogInfo("AdminAction", "Unban", $"Kullanıcı yasağı kaldırıldı - ID: {user.Id}, Kullanıcı: {user.UserName}");

        try
        {
            _notificationService.Add(new Notification
            {
                UserId = userId,
                Title = "Hesabınız yeniden aktif edildi",
                Message = "Hesabınıza uygulanan kısıtlama yöneticiler tarafından kaldırıldı.",
                Type = "AdminInfo",
                ReferenceLink = null
            });
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }

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
        _logService.LogWarning("AdminAction", "ToggleAdminRole", $"Admin rolü {(user.IsAdmin ? "verildi" : "kaldırıldı")} - ID: {user.Id}, Kullanıcı: {user.UserName}");
        _userDal.Update(user);
        try
        {
            _notificationService.Add(new Notification
            {
                UserId = userId,
                Title = $"Admin rolü {(user.IsAdmin ? "verildi" : "kaldırıldı")}",
                Message = user.IsAdmin
                    ? "Hesabınıza bir yönetici tarafından Admin yetkisi verildi."
                    : "Hesabınızdan Admin yetkisi kaldırıldı.",
                Type = "RoleChanged",
                ReferenceLink = null
            });
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        string action = user.IsAdmin ? "Admin rolü verildi" : "Admin rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }

    public IResult ToggleExpertRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsExpert = !user.IsExpert;
        _logService.LogWarning("AdminAction", "ToggleExpertRole", $"Uzman rolü {(user.IsExpert ? "verildi" : "kaldırıldı")} - ID: {user.Id}, Kullanıcı: {user.UserName}");
        _userDal.Update(user);
        try
        {
            _notificationService.Add(new Notification
            {
                UserId = userId,
                Title = $"Uzman rolü {(user.IsExpert ? "verildi" : "kaldırıldı")}",
                Message = user.IsExpert
                    ? "Hesabınıza bir yönetici tarafından Uzman yetkisi verildi."
                    : "Hesabınızdan Uzman yetkisi kaldırıldı.",
                Type = "RoleChanged",
                ReferenceLink = null
            });
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        string action = user.IsExpert ? "Uzman rolü verildi" : "Uzman rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }

    public IResult ToggleOfficialRole(int userId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);
        user.IsOfficial = !user.IsOfficial;
        _logService.LogWarning("AdminAction", "ToggleOfficialRole", $"Resmi rolü {(user.IsOfficial ? "verildi" : "kaldırıldı")} - ID: {user.Id}, Kullanıcı: {user.UserName}");
        _userDal.Update(user);
        try
        {
            _notificationService.Add(new Notification
            {
                UserId = userId,
                Title = $"Resmi Kurum yetkisi {(user.IsOfficial ? "verildi" : "kaldırıldı")}",
                Message = user.IsOfficial
                    ? "Hesabınıza bir yönetici tarafından Resmi Kurum yetkisi verildi."
                    : "Hesabınızdan Resmi Kurum yetkisi kaldırıldı.",
                Type = "RoleChanged",
                ReferenceLink = null
            });
        }
        catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        string action = user.IsOfficial ? "Resmi rolü verildi" : "Resmi rolü kaldırıldı";
        return new SuccessResult($"{action} (ID: {user.Id})");
    }
}