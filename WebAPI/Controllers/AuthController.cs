using Business.Abstract;
using Entities.DTOs;
using Entities.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IUserService _userService;

        public AuthController(IEmailVerificationService emailVerificationService, IUserService userService)
        {
            _emailVerificationService = emailVerificationService;
            _userService = userService;
        }

        [HttpPost("verifyemail")]
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
        public IActionResult ForgotPassword([FromBody] string email)
        {
            var userDetail = _userService.GetByEmail(email);
            if (userDetail == null) return BadRequest("Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.");

            var result = _emailVerificationService.SendPasswordResetCode(userDetail);
            if (result.Success) return Ok(result.Message);

            return BadRequest(result.Message);
        }

        [HttpPost("resetpassword")]
        public IActionResult ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var verifyResult = _emailVerificationService.Verify(resetPasswordDto.Email, resetPasswordDto.Code);
            if (!verifyResult.Success)
            {
                return BadRequest(verifyResult.Message);
            }

            var user = _userService.GetByEmail(resetPasswordDto.Email);

            var updateResult = _userService.ResetPassword(user.Id, resetPasswordDto.NewPassword);

            if (updateResult.Success) return Ok(updateResult.Message);

            return BadRequest(updateResult.Message);
        }

        [HttpPost("resendverification")]
        public IActionResult ResendVerification([FromBody] string email)
        {
            var userDetail = _userService.GetByEmail(email);
            if (userDetail == null) return BadRequest("Kullanıcı bulunamadı.");
            if (userDetail.IsEmailVerified) return BadRequest("Bu hesap zaten doğrulanmış.");

            var result = _emailVerificationService.SendVerificationCode(userDetail);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

    }
}