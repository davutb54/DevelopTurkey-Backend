using Core.Entities;
using Core.Entities.Constants;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ConstantController : Controller
	{
		[HttpGet("cities")]
		public IActionResult GetCities()
		{
			var result = new SuccessDataResult<List<City>>(ConstantData.Cities);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("genders")]
		public IActionResult GetGenders()
		{
			var result = new SuccessDataResult<List<Gender>>(ConstantData.Genders);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
