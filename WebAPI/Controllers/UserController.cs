using System.Diagnostics;
using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UserController : Controller
	{
		private readonly IUserService _userService = new UserManager();

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
			var result = _userService.Login(userForLoginDto);
			return Ok(result);
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
			var result = _userService.Register(userForRegisterDto);
			return Ok(result);
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
