using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IUserService _userService;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
        private readonly IWorkflowEventBus _eventBus;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ICaptchaService _captchaService;
        private readonly IValidator<UserForRegisterDto> _registerValidator;
        private readonly IInstitutionFeatureService _institutionFeatureService;
        private readonly ILogService _logService;
        private readonly IValidator<UserForPasswordUpdateDto> _passwordUpdateValidator;

        public AuthController(
            IEmailVerificationService emailVerificationService,
            IUserService userService,
            IValidator<ResetPasswordDto> resetPasswordValidator,
            IWorkflowEventBus eventBus,
            IWebHostEnvironment webHostEnvironment,
            ICaptchaService captchaService,
            IValidator<UserForRegisterDto> registerValidator,
            IInstitutionFeatureService institutionFeatureService,
            ILogService logService,
            IValidator<UserForPasswordUpdateDto> passwordUpdateValidator)
        {
            _emailVerificationService = emailVerificationService;
            _userService = userService;
            _resetPasswordValidator = resetPasswordValidator;
            _eventBus = eventBus;
            _webHostEnvironment = webHostEnvironment;
            _captchaService = captchaService;
            _registerValidator = registerValidator;
            _institutionFeatureService = institutionFeatureService;
            _logService = logService;
            _passwordUpdateValidator = passwordUpdateValidator;
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthLimit")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            if (!_webHostEnvironment.IsDevelopment())
            {
                var captchaResult = await _captchaService.VerifyCaptchaAsync(userForLoginDto.CaptchaToken ?? "");
                if (!captchaResult.Success)
                {
                    return BadRequest(captchaResult.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(userForLoginDto.UserName) || string.IsNullOrWhiteSpace(userForLoginDto.Password))
            {
                return BadRequest("Kullanıcı adı ve şifre boş olamaz.");
            }

            var userToLogin = _userService.Login(userForLoginDto);
            if (!userToLogin.Success)
            {
                var failIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logService.LogWarning("Auth", "Login", $"Başarısız giriş denemesi: {userForLoginDto.UserName} - IP: {failIp}");
                _ = _eventBus.PublishAsync("auth.login_failed", new RuleContext
                {
                    Metadata = new Dictionary<string, object?>
                    {
                        ["IpAddress"] = failIp,
                        ["UserAgent"] = Request.Headers.UserAgent.ToString(),
                        ["AttemptedUserName"] = userForLoginDto.UserName
                    }
                });
                return BadRequest(userToLogin.Message);
            }

            var user = _userService.GetByUserName(userForLoginDto.UserName)
                       ?? _userService.GetByEmail(userForLoginDto.UserName);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logService.LogInfo("Auth", "Login", $"Kullanıcı girişi: {user.UserName} - IP: {ipAddress}");

            int? sessionTimeout = null;
            var timeoutStr = _institutionFeatureService.GetFeatureValue(user.InstitutionId, "Identity.SessionTimeoutMinutes", "");
            if (!string.IsNullOrEmpty(timeoutStr) && int.TryParse(timeoutStr, out int parsedTimeout) && parsedTimeout > 0)
            {
                sessionTimeout = parsedTimeout;
            }

            var result = _userService.CreateAccessToken(user, null, sessionTimeout);
            if (result.Success)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = _webHostEnvironment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                    Expires = result.Data.Expiration
                };

                Response.Cookies.Append("token", result.Data.Token, cookieOptions);
                Response.Cookies.Append("userId", result.Data.UserId.ToString(), cookieOptions);

                _ = _eventBus.PublishAsync("auth.login_success", new RuleContext
                {
                    SystemUserId = user.Id,
                    InstitutionId = user.InstitutionId,
                    Metadata = new Dictionary<string, object?> { ["IpAddress"] = ipAddress }
                });

                return Ok(new { success = true, message = "Giriş başarılı." });
            }

            return BadRequest(result.Message);
        }

        [HttpPost("google-login")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult GoogleLogin([FromBody] UserForGoogleLoginDto dto)
        {
            var result = _userService.GoogleLogin(dto);
            if (result.Success)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = _webHostEnvironment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                    Expires = result.Data.Expiration
                };
                Response.Cookies.Append("token", result.Data.Token, cookieOptions);
                Response.Cookies.Append("userId", result.Data.UserId.ToString(), cookieOptions);
                return Ok(new { success = true, message = "Google ile giriş başarılı." });
            }
            return BadRequest(result.Message);
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthLimit")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            if (!_webHostEnvironment.IsDevelopment())
            {
                var captchaResult = await _captchaService.VerifyCaptchaAsync(userForRegisterDto.CaptchaToken ?? "");
                if (!captchaResult.Success)
                {
                    return BadRequest(captchaResult.Message);
                }
            }

            var validationResult = _registerValidator.Validate(userForRegisterDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            if (!userForRegisterDto.AgreementAccepted)
            {
                return BadRequest("Kayıt olabilmek için kullanım koşullarını kabul etmeniz gerekmektedir.");
            }

            var userExists = _userService.CheckUserExists(new CheckExistsDto
            {
                Email = userForRegisterDto.Email,
                Username = userForRegisterDto.UserName
            });

            if (userExists.Success)
            {
                return BadRequest(userExists.Message);
            }

            var registerResult = _userService.Register(userForRegisterDto);

            if (registerResult.Success)
            {
                var user = _userService.GetByUserName(userForRegisterDto.UserName);
                var tokenResult = _userService.CreateAccessToken(user);

                var emailResult = _emailVerificationService.SendVerificationCode(user);

                if (!emailResult.Success)
                {
                    _logService.LogWarning("Auth", "Register_Email_Failed", $"Kayıt başarılı ancak e-posta gönderilemedi. User: {user.UserName}");
                    return Ok(tokenResult.Data);
                }

                if (tokenResult.Success)
                {
                    _logService.LogInfo("Auth", "Register", $"Yeni kullanıcı başarıyla kayıt oldu. User: {user.UserName}");

                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = _webHostEnvironment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                        Expires = tokenResult.Data.Expiration
                    };

                    Response.Cookies.Append("token", tokenResult.Data.Token, cookieOptions);
                    Response.Cookies.Append("userId", tokenResult.Data.UserId.ToString(), cookieOptions);

                    return Ok(new { success = true, message = "Kayıt başarılı." });
                }

                return Ok(registerResult.Message);
            }

            return BadRequest(registerResult.Message);
        }

        [HttpPost("updatepassword")]
        [EnableRateLimiting("AuthLimit")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            if (User.Claims.Any(c => c.Type == ClaimTypes.Actor))
            {
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının şifresini değiştiremezsiniz.");
            }

            var validationResult = _passwordUpdateValidator.Validate(userForPasswordUpdateDto);
            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var result = _userService.UpdatePassword(userForPasswordUpdateDto);
            return Ok(result);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = _webHostEnvironment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(-1)
            };

            Response.Cookies.Append("token", "", cookieOptions);
            Response.Cookies.Append("userId", "", cookieOptions);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int logoutUserId = userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid) ? uid : 0;
            _ = _eventBus.PublishAsync("auth.logout", new RuleContext { SystemUserId = logoutUserId });

            return Ok(new { success = true, message = "Çıkış başarılı." });
        }

        [HttpPost("revertimpersonation")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult RevertImpersonation()
        {
            var actorClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor);
            if (actorClaim == null)
            {
                return BadRequest("Bu işlem için aktif bir sudo oturumunuz bulunmamaktadır.");
            }

            int adminId = int.Parse(actorClaim.Value);
            var adminUserDto = _userService.GetById(adminId);
            if (!adminUserDto.Success || adminUserDto.Data == null) return BadRequest("Orijinal hesap bulunamadı.");

            var adminUser = _userService.GetByUserName(adminUserDto.Data.UserName);

            var tokenResult = _userService.CreateAccessToken(adminUser);
            if (!tokenResult.Success) return BadRequest(tokenResult.Message);

            _ = _eventBus.PublishAsync("auth.impersonation_reverted", new RuleContext
            {
                SystemUserId = adminId,
                Metadata = new Dictionary<string, object?>
                {
                    ["AdminId"] = adminId
                }
            });

            _logService.LogInfo("Security", "RevertImpersonation", $"Admin (ID:{adminId}) kimliğine geri dönüş sağladı.");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = _webHostEnvironment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                Expires = tokenResult.Data.Expiration
            };

            Response.Cookies.Append("token", tokenResult.Data.Token, cookieOptions);
            Response.Cookies.Append("userId", tokenResult.Data.UserId.ToString(), cookieOptions);

            return Ok(new { success = true, data = tokenResult.Data, message = "Admin hesabına başarıyla geri dönüldü." });
        }

        [HttpPost("verifyemail")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult VerifyEmail([FromBody] VerifyEmailDto verifyEmailDto)
        {
            var result = _emailVerificationService.Verify(verifyEmailDto.Email, verifyEmailDto.Code);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }

        [HttpPost("forgotpassword")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult ForgotPassword([FromBody] string email)
        {
            var userDetail = _userService.GetByEmail(email);
            if (userDetail == null) return BadRequest("Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.");

            var result = _emailVerificationService.SendPasswordResetCode(userDetail);
            if (result.Success) return Ok(result.Message);

            return BadRequest(result.Message);
        }

        [HttpPost("resetpassword")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var validationResult = _resetPasswordValidator.Validate(resetPasswordDto);
            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var verifyResult = _emailVerificationService.VerifyForResetPassword(resetPasswordDto.Email, resetPasswordDto.Code);
            if (!verifyResult.Success)
            {
                return BadRequest(verifyResult.Message);
            }

            var user = _userService.GetByEmail(resetPasswordDto.Email);

            var updateResult = _userService.ResetPassword(user.Id, resetPasswordDto.NewPassword);

            if (updateResult.Success)
            {
                _ = _eventBus.PublishAsync("auth.password_reset_completed", new RuleContext
                {
                    SystemUserId = user.Id
                });

                return Ok(updateResult.Message);
            }

            return BadRequest(updateResult.Message);
        }

        [HttpPost("resendverification")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult ResendVerification([FromBody] string email)
        {
            var userDetail = _userService.GetByEmail(email);
            if (userDetail == null) return BadRequest("Kullanıcı bulunamadı.");
            if (userDetail.IsEmailVerified) return BadRequest("Bu hesap zaten doğrulanmış.");

            var result = _emailVerificationService.SendVerificationCode(userDetail);
            if (result.Success)
            {
                _ = _eventBus.PublishAsync("auth.verification_resent", new RuleContext
                {
                    SystemUserId = userDetail.Id
                });

                return Ok(result.Message);
            }

            return BadRequest(result.Message);
        }
    }
}
