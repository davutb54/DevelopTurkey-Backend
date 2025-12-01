using Business.Abstract;
using Business.Concrete;
using Core.Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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
                return BadRequest(userToLogin.Message);
            }

            var user = _userService.GetByUserName(userForLoginDto.UserName);

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
    }
}
