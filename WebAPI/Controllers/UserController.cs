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
                var emailResult = _emailVerificationService.SendVerificationCode(user);

                if (!emailResult.Success)
                {
                    return BadRequest(emailResult.Message);
                }

                if (result.Success)
                {
                    return Ok(result.Data);
                }

                return Ok(registerResult.Message);
            }

            return BadRequest(registerResult.Message);
        }


        [HttpPost("updatedetails")]
        public IActionResult UpdateDetails(UserForUpdateDto userForUpdateDto)
        {
            var result = _userService.UpdateUserDetails(userForUpdateDto);
            return Ok(result);
        }

        [HttpPost("delete")]
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
            var userDetail = _userService.GetById(userImageUpdateDto.UserId);
            if (!userDetail.Success) return BadRequest(userDetail.Message);

            var extension = Path.GetExtension(userImageUpdateDto.Image.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Sadece .jpg, .jpeg, .png veya .webp formatında resim yükleyebilirsiniz!");
            }

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");

            string newFileName = Core.Utilities.Helpers.FileHelper.FileHelper.Add(userImageUpdateDto.Image, uploadPath);

            if (newFileName == null) return BadRequest("Dosya yüklenemedi.");

            var user = _userService.GetByUserName(userDetail.Data.UserName);

            user.ProfileImageUrl = newFileName;
            _userService.Update(user);

            return Ok(new { message = "Profil resmi güncellendi", imageUrl = newFileName });
        }
    }
}
