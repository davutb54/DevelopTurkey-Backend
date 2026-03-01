using Business.Abstract;
using Business.Concrete;
using Core.Entities.Concrete;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

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

        public UserController(
            IUserService userService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<UserForRegisterDto> registerValidator,IEmailVerificationService emailVerificationService, ILogService logService)
        {
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
            _registerValidator = registerValidator;
            _emailVerificationService = emailVerificationService;
            _logService = logService;
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

        [HttpPost("login")]
        public IActionResult Login(UserForLoginDto userForLoginDto)
        {
           
            if (string.IsNullOrWhiteSpace(userForLoginDto.UserName) || string.IsNullOrWhiteSpace(userForLoginDto.Password))
            {
                return BadRequest("Kullanıcı adı ve şifre boş olamaz.");
            }

            var userToLogin = _userService.Login(userForLoginDto);
            if (!userToLogin.Success)
            {
                var failIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logService.Add(new Log
                {
                    Message = $"Başarısız Giriş Denemesi: {userForLoginDto.UserName} kullanıcısı için yanlış şifre girildi. IP: {failIp}",
                    CreationDate = DateTime.Now,
                    Type = "user,loginIP,Error"
                });
                return BadRequest(userToLogin.Message);
            }

            var user = _userService.GetByUserName(userForLoginDto.UserName);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logService.Add(new Log
            {
                Message = $"Kullanıcı Girişi: {user.UserName} adlı kullanıcı sisteme giriş yaptı. IP: {ipAddress}",
                CreationDate = DateTime.Now,
                Type = "user,loginIP,OK"
            });

            var result = _userService.CreateAccessToken(user);
            if (result.Success)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.Message);
        }

        [HttpPost("updatepassword")]
        public IActionResult UpdatePassword(UserForPasswordUpdateDto userForPasswordUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
            {
                return Unauthorized("Geçersiz token.");
            }

            userForPasswordUpdateDto.Id = Convert.ToInt32(userIdClaim.Value);

            var result = _userService.UpdatePassword(userForPasswordUpdateDto);
            return Ok(result);
        }

        [HttpPost("register")]
        public IActionResult Register(UserForRegisterDto userForRegisterDto)
        {
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
            var result = _userService.CreateAccessToken(
                new User
                {
                    UserName = userForRegisterDto.UserName,
                    Email = userForRegisterDto.Email,
                    Id = 0
                }
            );

            if (registerResult.Success)
            {
                var user = _userService.GetByUserName(userForRegisterDto.UserName);
                var tokenResult = _userService.CreateAccessToken(user);

                var emailResult = _emailVerificationService.SendVerificationCode(user);

                if (!emailResult.Success)
                {
                    _logService.Add(new Log
                    {
                        Message = $"Kayıt başarılı ancak doğrulama e-postası gönderilemedi. User: {user.UserName}",
                        CreationDate = DateTime.Now,
                        Type = "user,register,EmailWarning"
                    });

                    return Ok(result);
                }

                if (tokenResult.Success)
                {
                    return Ok(tokenResult.Data);
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

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
            {
                return Unauthorized("Geçersiz token.");
            }

            userForUpdateDto.Id = Convert.ToInt32(userIdClaim.Value);

            var result = _userService.UpdateUserDetails(userForUpdateDto);
            return Ok(result);
        }

        [HttpPost("delete")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            var isAdminClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Admin");
            if (isAdminClaim == null)
            {
                return Forbid("Bu işlemi yapma yetkiniz yok. Sadece adminler kullanıcı silebilir.");
            }

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

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
            {
                return Unauthorized("Geçersiz token.");
            }

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
    }
}
