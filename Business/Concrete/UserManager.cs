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
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Business.Models;

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
    private readonly IConfiguration _configuration;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IInstitutionFeatureService _institutionFeatureService;
    private readonly IWorkflowEventBus _eventBus;

    public UserManager(IUserDal userDal, ILogService logService, ITokenHelper tokenHelper, IInstitutionService institutionService, IClientContext clientContext, ISystemSettingsService systemSettingsService, INotificationService notificationService, IConfiguration configuration, IEmailVerificationService emailVerificationService, IInstitutionFeatureService institutionFeatureService, IWorkflowEventBus eventBus)
    {
        _userDal = userDal;
        _logService = logService;
        _tokenHelper = tokenHelper;
        _institutionService = institutionService;
        _clientContext = clientContext;
        _systemSettingsService = systemSettingsService;
        _notificationService = notificationService;
        _configuration = configuration;
        _emailVerificationService = emailVerificationService;
        _institutionFeatureService = institutionFeatureService;
        _eventBus = eventBus;
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

    public IDataResult<UserPublicProfileDto?> GetPublicProfile(int id, int institutionId)
    {
        var user = _userDal.GetUserDetail(u => u.Id == id && u.InstitutionId == institutionId);

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
                RegisterDate = user.RegisterDate,
                ProfileImageUrl = user.ProfileImageUrl,
                InstitutionId = user.InstitutionId,
                IsProfilePublic = user.IsProfilePublic,
                ShowSolutions = user.ShowSolutions,
                ShowProblems = user.ShowProblems
            };
            return new SuccessDataResult<UserPublicProfileDto?>(publicProfile, Messages.UserGetByIdOk);
        }

        return new ErrorDataResult<UserPublicProfileDto?>(null, Messages.UserGetByIdError);
    }

    public IDataResult<UserPublicProfileDto?> GetPublicProfileByUserName(string username, int institutionId)
    {
        var user = _userDal.GetUserDetail(u => u.UserName == username && u.InstitutionId == institutionId);

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
                RegisterDate = user.RegisterDate,
                ProfileImageUrl = user.ProfileImageUrl,
                InstitutionId = user.InstitutionId,
                IsProfilePublic = user.IsProfilePublic,
                ShowSolutions = user.ShowSolutions,
                ShowProblems = user.ShowProblems
            };
            return new SuccessDataResult<UserPublicProfileDto?>(publicProfile, Messages.UserGetByIdOk);
        }

        return new ErrorDataResult<UserPublicProfileDto?>(null, Messages.UserGetByIdError);
    }

    public IDataResult<List<UserDetailDto>> GetAll()
    {
        return new SuccessDataResult<List<UserDetailDto>>(_userDal.GetUserDetails(), Messages.UserGetAllOk);
    }

    public IDataResult<List<UserDetailDto>> GetAllByInstitutions(IEnumerable<int> institutionIds)
    {
        var ids = institutionIds.ToHashSet();
        var users = _userDal.GetUserDetails(u => ids.Contains(u.InstitutionId));
        return new SuccessDataResult<List<UserDetailDto>>(users, Messages.UserGetAllOk);
    }

    public IDataResult<(List<UserDetailDto> Items, int TotalCount)> GetAllPaged(UserFilterDto filter)
    {
        var result = _userDal.GetUserDetailsPaged(filter);
        return new SuccessDataResult<(List<UserDetailDto> Items, int TotalCount)>(result, Messages.UserGetAllOk);
    }
    public IResult Login(UserForLoginDto userForLoginDto)
    {
        // Tenant filter atlanır — kullanıcı hangi kuruma ait olursa olsun bulunabilmeli.
        var user = _userDal.GetForAuth(u => u.UserName == userForLoginDto.UserName || u.Email == userForLoginDto.UserName);

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

        // Feature: MaxLoginAttempts - hesap kilitleme kontrolü
        int maxLoginAttempts = int.Parse(_institutionFeatureService.GetFeatureValue(user.InstitutionId, "Identity.MaxLoginAttempts", "5"));
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
        {
            var remainingMinutes = (int)(user.LockoutEnd.Value - DateTime.Now).TotalMinutes + 1;
            _logService.LogWarning("Security", "Login", $"Kilitli hesap giriş denemesi: {user.UserName} (Kalan: {remainingMinutes} dk)");
            return new ErrorResult($"Hesabınız çok fazla başarısız giriş denemesi nedeniyle {remainingMinutes} dakika süreyle kilitlenmiştir.");
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
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= maxLoginAttempts)
            {
                // Katlanarak artan blokaj (15dk, 30dk, 60dk, 120dk...)
                int lockoutMinutes = 15 * (int)Math.Pow(2, user.LockoutCount);
                if (lockoutMinutes > 1440) lockoutMinutes = 1440; // Max 24 saat

                user.LockoutEnd = DateTime.Now.AddMinutes(lockoutMinutes);
                user.LockoutCount++;
                user.FailedLoginAttempts = 0;
                _userDal.Update(user);
                
                _ = _eventBus.PublishAsync("auth.locked_out", new RuleContext
                {
                    SystemUserId = user.Id,
                    InstitutionId = user.InstitutionId,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["LockoutMinutes"] = lockoutMinutes,
                        ["LockoutCount"] = user.LockoutCount
                    }
                });

                _logService.LogWarning("Security", "Login", $"Hesap kilitlendi: {user.UserName} (Kilit Sayısı: {user.LockoutCount}, Süre: {lockoutMinutes} dk)");
                return new ErrorResult($"Hesabınız çok fazla başarısız giriş denemesi nedeniyle {lockoutMinutes} dakika süreyle kilitlenmiştir.");
            }
            _userDal.Update(user);
            _logService.LogWarning("Auth", "Login", $"Hatalı şifre girişi: {user.UserName} (Deneme: {user.FailedLoginAttempts}/{maxLoginAttempts})");
            return new ErrorResult(Messages.UserPasswordError);
        }

        // Feature: Email doğrulama kontrolü
        if (_institutionFeatureService.IsFeatureEnabled(user.InstitutionId, "Identity.RequireEmailVerification", true))
        {
            if (!user.IsEmailVerified)
            {
                _logService.LogWarning("Auth", "Login", $"Doğrulanmamış email ile giriş denemesi: {user.UserName}");
                return new ErrorResult("Giriş yapabilmek için e-posta adresinizi doğrulamanız gerekmektedir.");
            }
        }

        // Başarılı giriş: sayaçları sıfırla
        user.FailedLoginAttempts = 0;
        user.LockoutCount = 0; // Kilit sayısını da sıfırla
        user.LockoutEnd = null;
        _userDal.Update(user);

        _logService.LogInfo("Auth", "Login", $"Başarılı giriş - ID: {user.Id}, Kullanıcı: {user.UserName}");
        return new SuccessResult(Messages.UserLoginOk);
    }

    public IDataResult<AccessToken> GoogleLogin(UserForGoogleLoginDto googleLoginDto)
    {
        GoogleJsonWebSignature.Payload payload;
        try {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["GoogleAuth:ClientId"] }
            };
            // Token doğrulaması
            payload = GoogleJsonWebSignature.ValidateAsync(googleLoginDto.Credential, validationSettings).Result;
        } catch {
            return new ErrorDataResult<AccessToken>(null, "Google yetkilendirmesi geçersiz veya bu uygulama için üretilmemiş.");
        }

        var user = _userDal.GetForAuth(u => u.Email == payload.Email);

        if (user == null)
        {
            // YENİ KAYIT: Mevcut Register metodundaki Institution atama mantığını kullan
            string emailDomain = payload.Email.Split('@')[1].ToLower();
            var institutionResult = _institutionService.GetByDomain(emailDomain);
            int assignedInstitutionId = (institutionResult.Success && institutionResult.Data != null && institutionResult.Data.Status)
                ? institutionResult.Data.Id : 1;

            // Benzersiz Username üret (Örn: isim.soyisim veya email prefixi)
            string baseUsername = payload.Email.Split('@')[0];
            string uniqueUsername = baseUsername;
            int counter = 1;
            while (_userDal.GetForAuth(u => u.UserName == uniqueUsername) != null) {
                uniqueUsername = $"{baseUsername}{counter++}";
            }

            user = new User {
                UserName = uniqueUsername,
                Name = payload.GivenName ?? "Kullanıcı",
                Surname = payload.FamilyName ?? "",
                Email = payload.Email,
                ProfileImageUrl = payload.Picture,
                PasswordHash = null,
                PasswordSalt = null,
                AuthType = "Google",
                IsEmailVerified = true, // Google'dan geldiği için doğrulanmış kabul edilir
                InstitutionId = assignedInstitutionId,
                RegisterDate = DateTime.Now,
                CityCode = 0,
                Gender = 0
            };
            _userDal.Add(user);
            _ = _eventBus.PublishAsync("auth.registered", new RuleContext
            {
                SystemUserId = user.Id,
                InstitutionId = user.InstitutionId
            });
            _logService.LogInfo("Auth", "GoogleRegister", $"Google ile yeni kayıt: {user.Email}");
        }

        // GİRİŞ İŞLEMİ (Hesap birleştirilmiş veya yeni açılmış fark etmez)
        if (user.IsBanned) return new ErrorDataResult<AccessToken>(null, "Hesabınız askıya alınmıştır.");

        _ = _eventBus.PublishAsync("auth.google_login", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

        _logService.LogInfo("Auth", "GoogleLogin", $"Google ile giriş: {user.Email}");
        var accessToken = _tokenHelper.CreateToken(user, null);
        accessToken.UserId = user.Id;
        return new SuccessDataResult<AccessToken>(accessToken, "Giriş başarılı.");
    }

    public IDataResult<AccessToken> CreateAccessToken(User user, int? impersonatedById = null, int? sessionTimeoutMinutes = null)
    {
        if (_tokenHelper == null) return new ErrorDataResult<AccessToken>(null, "Token servisi yapılandırılmadı.");

        var accessToken = _tokenHelper.CreateToken(user, impersonatedById, sessionTimeoutMinutes);
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
            : (userForRegisterDto.InstitutionId ?? 1);

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
            IsDeleted = false,
            IsReported = false,
            IsBanned = false,
            IsEmailVerified = false,
            InstitutionId = assignedInstitutionId
        };

        _userDal.Add(user);

        _ = _eventBus.PublishAsync("auth.registered", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

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

        // Google OAuth kullanıcısı ve henüz şifresi yoksa eski şifre kontrolünü atla
        bool isGoogleUserWithNoPassword = user.AuthType == "Google" && user.PasswordHash == null;
        if (!isGoogleUserWithNoPassword)
        {
            if (!HashingHelper.VerifyPasswordHash(userForPasswordUpdateDto.OldPassword, user.PasswordHash, user.PasswordSalt))
            {
                _logService.LogWarning("Security", "UpdatePassword", $"Hatalı eski şifre girişi - ID: {user.Id}, Kullanıcı: {user.UserName}");
                return new ErrorResult(Messages.UserPasswordError);
            }
        }

        byte[] newHash, newSalt;
        HashingHelper.CreatePasswordHash(userForPasswordUpdateDto.NewPassword, out newHash, out newSalt);

        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;

        _userDal.Update(user);

        _ = _eventBus.PublishAsync("auth.password_changed", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

        _logService.LogInfo("Security", "UpdatePassword", $"Şifre güncellendi - ID: {user.Id}, Kullanıcı: {user.UserName}");
        return new SuccessResult(Messages.UserPasswordUpdateOk);
    }

    public IResult CheckUserExists(CheckExistsDto checkExistsDto)
    {
        // Tüm kurumlar içinde benzersizlik kontrolü yapılmalı (tenant filter atlanır).
        var emailUser = _userDal.GetForAuth(u => u.Email == checkExistsDto.Email);
        var usernameUser = _userDal.GetForAuth(u => u.UserName == checkExistsDto.Username);

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

        // E-posta adresi değiştiyse doğrulama sıfırla ve yeniden gönder
        bool emailChanged = !string.Equals(user.Email, userForUpdateDto.Email, StringComparison.OrdinalIgnoreCase);

        user.Name = userForUpdateDto.Name;
        user.Surname = userForUpdateDto.Surname;
        user.Email = userForUpdateDto.Email;
        user.CityCode = userForUpdateDto.CityCode;
        user.Gender = userForUpdateDto.GenderCode;
        user.CustomHierarchyId = userForUpdateDto.CustomHierarchyId;
        user.MentionNotificationEnabled = userForUpdateDto.MentionNotificationEnabled;
        user.IsProfilePublic = userForUpdateDto.IsProfilePublic;
        user.ShowSolutions = userForUpdateDto.ShowSolutions;
        user.ShowProblems = userForUpdateDto.ShowProblems;

        if (emailChanged)
        {
            user.IsEmailVerified = false;
            _userDal.Update(user);
            _ = _emailVerificationService.SendVerificationCode(user);
            _logService.LogWarning("Auth", "UpdateDetails", $"E-posta değiştirildi, doğrulama sıfırlandı - ID: {user.Id}, Yeni E-posta: {user.Email}");
        }
        else
        {
            _userDal.Update(user);
        }

        _logService.LogInfo("Auth", "UpdateDetails", $"Kullanıcı bilgileri güncellendi - ID: {user.Id}, Kullanıcı: {user.UserName}");

        _ = _eventBus.PublishAsync("user.updated", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

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

        _ = _eventBus.PublishAsync("user.deleted", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

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

    public User? GetByUserNameForAuth(string userName)
    {
        return _userDal.GetForAuth(u => u.UserName == userName);
    }

    public User? GetByEmailForAuth(string email)
    {
        return _userDal.GetForAuth(u => u.Email == email);
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

        _ = _eventBus.PublishAsync("user.banned", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

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

        _ = _eventBus.PublishAsync("user.unbanned", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

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

        _ = _eventBus.PublishAsync("user.reported", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId
        });

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

            _ = _eventBus.PublishAsync("user.unreported", new RuleContext
            {
                SystemUserId = user.Id,
                InstitutionId = user.InstitutionId
            });
        }
        return new SuccessResult();
    }


    public IResult ChangeUserInstitution(int userId, int newInstitutionId)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        int oldInstitutionId = user.InstitutionId;
        if (oldInstitutionId == newInstitutionId)
            return new SuccessResult("Kullanıcı zaten bu kuruma üye.");

        user.InstitutionId = newInstitutionId;
        _userDal.Update(user);

        _logService.LogWarning("AdminAction", "ChangeInstitution",
            $"Kullanıcı kurumu değiştirildi - ID: {user.Id}, Eski: {oldInstitutionId}, Yeni: {newInstitutionId}");

        _ = _eventBus.PublishAsync("user.institution_changed", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = newInstitutionId,
            OldValue = oldInstitutionId.ToString(),
            NewValue = newInstitutionId.ToString(),
            Metadata = new Dictionary<string, object?>
            {
                ["OldValue"] = oldInstitutionId.ToString(),
                ["NewValue"] = newInstitutionId.ToString(),
                ["OldInstitutionId"] = oldInstitutionId,
                ["NewInstitutionId"] = newInstitutionId
            }
        });

        return new SuccessResult($"Kullanıcı (ID: {userId}) kurumu {oldInstitutionId} -> {newInstitutionId} olarak güncellendi.");
    }

    public IResult UpdateUsername(int userId, string newUsername)
    {
        var user = _userDal.Get(u => u.Id == userId);
        if (user == null) return new ErrorResult(Messages.UserNotFound);

        // Kurum ayarı kontrolü
        bool allowUsernameChange = _institutionFeatureService.IsFeatureEnabled(user.InstitutionId, "Profile.AllowUsernameChange", true);
        if (!allowUsernameChange)
            return new ErrorResult("Kurumunuz kullanıcı adı değişikliğine izin vermemektedir.");

        // Mevcut kullanıcı adıyla aynıysa başarılı dön
        if (string.Equals(user.UserName, newUsername, StringComparison.OrdinalIgnoreCase))
            return new SuccessResult("Kullanıcı adınız zaten bu.");

        // Başkası bu kullanıcı adını kullanıyor mu?
        var existing = _userDal.Get(u => u.UserName == newUsername);
        if (existing != null)
            return new ErrorResult("Bu kullanıcı adı zaten kullanılmaktadır.");

        // 30 günlük bekleme süresi kontrolü
        if (user.LastUsernameChangeDate.HasValue &&
            (DateTime.Now - user.LastUsernameChangeDate.Value).TotalDays < 30)
        {
            int remainingDays = 30 - (int)(DateTime.Now - user.LastUsernameChangeDate.Value).TotalDays;
            return new ErrorResult($"Kullanıcı adınızı 30 günde bir değiştirebilirsiniz. {remainingDays} gün daha beklemeniz gerekmektedir.");
        }

        string oldUsername = user.UserName;
        user.UserName = newUsername;
        user.LastUsernameChangeDate = DateTime.Now;
        _userDal.Update(user);

        _ = _eventBus.PublishAsync("user.username_changed", new RuleContext
        {
            SystemUserId = user.Id,
            InstitutionId = user.InstitutionId,
            OldValue = oldUsername,
            NewValue = newUsername,
            Metadata = new Dictionary<string, object?>
            {
                ["OldUsername"] = oldUsername,
                ["NewUsername"] = newUsername
            }
        });

        _logService.LogInfo("Auth", "UpdateUsername", $"Kullanıcı adı güncellendi - ID: {user.Id}, Eski: {oldUsername}, Yeni: {newUsername}");
        return new SuccessResult("Kullanıcı adınız başarıyla güncellendi.");
    }
}