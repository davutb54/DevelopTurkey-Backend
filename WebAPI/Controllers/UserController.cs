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

        public UserController(
            IUserService userService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<UserForRegisterDto> registerValidator, IEmailVerificationService emailVerificationService, ILogService logService,
            ICaptchaService captchaService)
        {
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
            _registerValidator = registerValidator;
            _emailVerificationService = emailVerificationService;
            _logService = logService;
            _captchaService = captchaService;
        }

        [HttpGet("getbyid")]
        public IActionResult GetById(int id)
        {
            var result = _userService.GetById(id);
            return Ok(result);
        }

        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            var result = _userService.GetAll();
            return Ok(result);
        }

        [HttpGet("me")]
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
        public IActionResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

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
        public IActionResult UpdateDetails(UserForUpdateDto userForUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
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
        public IActionResult CheckUserExists(CheckExistsDto checkExistsDto)
        {
            var result = _userService.CheckUserExists(checkExistsDto);
            return Ok(result);
        }

        [HttpPost("uploadprofileimage")]
        public IActionResult UploadProfileImage([FromForm] UserImageUpdateDto userImageUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
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

    }
}
