using Business.Abstract;
using Business.Concrete;
using Core.Entities.Concrete;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IValidator<UserForRegisterDto> _registerValidator;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly ILogService _logService;
        private readonly ICaptchaService _captchaService;
        private readonly IValidator<UserForPasswordUpdateDto> _passwordUpdateValidator;

        public UserController(
            IUserService userService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<UserForRegisterDto> registerValidator, IEmailVerificationService emailVerificationService, ILogService logService,
            ICaptchaService captchaService, IValidator<UserForPasswordUpdateDto> passwordUpdateValidator)
        {
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
            _registerValidator = registerValidator;
            _emailVerificationService = emailVerificationService;
            _logService = logService;
            _captchaService = captchaService;
            _passwordUpdateValidator = passwordUpdateValidator;
        }

        [HttpGet("getbyid")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetById(int id)
        {
            var result = _userService.GetById(id);
            return Ok(result);
        }

        [HttpGet("getpublicprofile")]
        public IActionResult GetPublicProfile(int id)
        {
            var result = _userService.GetPublicProfile(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getall")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult GetAll()
        {
            var result = _userService.GetAll();
            return Ok(result);
        }

        [HttpGet("getallpaged")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult GetAllPaged([FromQuery] UserFilterDto filter)
        {
            var result = _userService.GetAllPaged(filter);
            if (!result.Success) return BadRequest(result);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data.Items,
                totalCount = result.Data.TotalCount,
                page = filter.Page,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling((double)result.Data.TotalCount / filter.PageSize)
            });
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetMe()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
            {
                var result = _userService.GetById(id);
                return Ok(result);
            }

            return Unauthorized("Geçersiz token.");
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthLimit")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            // Captcha Doğrulaması (Production'da zorunlu, Development'ta geç)
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
                return BadRequest(userToLogin.Message);
            }

            var user = _userService.GetByUserName(userForLoginDto.UserName);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logService.LogInfo("Auth", "Login", $"Kullanıcı girişi: {user.UserName} - IP: {ipAddress}");

            var result = _userService.CreateAccessToken(user);
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

                return Ok(new { success = true, message = "Giriş başarılı." });
            }

            return BadRequest(result.Message);
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

            if (User.Claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Actor))
            {
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının şifresini değiştiremezsiniz.");
            }

            var validationResult = _passwordUpdateValidator.Validate(userForPasswordUpdateDto);
            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            // userForPasswordUpdateDto.Id = _clientContext.GetUserId() will be set in Manager
            var result = _userService.UpdatePassword(userForPasswordUpdateDto);
            return Ok(result);
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthLimit")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // Captcha Doğrulaması (Production'da zorunlu, Development'ta geç)
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


        [HttpPost("updatedetails")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult UpdateDetails(UserForUpdateDto userForUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            if (User.Claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Actor))
            {
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının profil detaylarını güncelleyemezsiniz.");
            }

            // userForUpdateDto.Id = _clientContext.GetUserId() will be set in Manager
            var result = _userService.UpdateUserDetails(userForUpdateDto);
            return Ok(result);
        }

        [HttpPost("delete")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var result = _userService.DeleteUser(id);
            return Ok(result);
        }

        [HttpPost("checkuserexists")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult CheckUserExists(CheckExistsDto checkExistsDto)
        {
            var result = _userService.CheckUserExists(checkExistsDto);
            return Ok(result);
        }

        [HttpPost("uploadprofileimage")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult UploadProfileImage([FromForm] UserImageUpdateDto userImageUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            if (User.Claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Actor))
            {
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının profil resmini güncelleyemezsiniz.");
            }

            // ClientContext is not easily accessible here without DI, but we can rely on standard token claim since we removed it from business
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null) return Unauthorized("Geçersiz token.");
            int authenticatedUserId = Convert.ToInt32(userIdClaim.Value);

            var userDetail = _userService.GetById(authenticatedUserId);
            if (!userDetail.Success) return BadRequest(userDetail.Message);

            if (userImageUpdateDto.Image == null || userImageUpdateDto.Image.Length == 0)
            {
                return BadRequest("Lütfen bir resim dosyası seçin.");
            }

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");

            try
            {
                string newFileName = Core.Utilities.Helpers.FileHelper.FileHelper.Add(userImageUpdateDto.Image, uploadPath);

                if (newFileName == null) return BadRequest("Dosya yüklenemedi.");

                var user = _userService.GetByUserName(userDetail.Data.UserName);

                user.ProfileImageUrl = newFileName;
                _userService.Update(user);

                return Ok(new { message = "Profil resmi güncellendi", imageUrl = newFileName });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Dosya yüklenirken bir hata oluştu.");
            }
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

            return Ok(new { success = true, message = "Çıkış başarılı." });
        }

        [HttpPost("revertimpersonation")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult RevertImpersonation()
        {
            var actorClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Actor);
            if (actorClaim == null)
            {
                return BadRequest("Bu işlem için aktif bir sudo oturumunuz bulunmamaktadır.");
            }

            int adminId = int.Parse(actorClaim.Value);
            var adminUserDto = _userService.GetById(adminId);
            if (!adminUserDto.Success || adminUserDto.Data == null) return BadRequest("Orijinal hesap bulunamadı.");

            var adminUser = _userService.GetByUserName(adminUserDto.Data.UserName);
            
            // Yeni token üret ve içine SUDO işareti KOYMA.
            var tokenResult = _userService.CreateAccessToken(adminUser);
            if (!tokenResult.Success) return BadRequest(tokenResult.Message);

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

    }
}
