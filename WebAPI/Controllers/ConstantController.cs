using Core.Entities;
using Core.Entities.Constants;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;
using Entities.DTOs.Geo;
using WebAPI.Services;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ConstantController : Controller
	{
		private readonly IGeoLocationService _geoLocationService;

		public ConstantController(IGeoLocationService geoLocationService)
		{
			_geoLocationService = geoLocationService;
		}

		[HttpGet("cities")]
		public IActionResult GetCities()
		{
			var result = new SuccessDataResult<List<City>>(ConstantData.Cities);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("reverse-geocode")]
		public async Task<IActionResult> ReverseGeocode([FromQuery] double lat, [FromQuery] double lng, CancellationToken cancellationToken)
		{
			ReverseGeocodeCityDto? resolved = await _geoLocationService.ReverseGeocodeCityAsync(lat, lng, cancellationToken);
			if (resolved == null)
			{
				return BadRequest(new ErrorResult("Konumdan şehir tespit edilemedi. Lütfen pini şehir içinde olacak şekilde düzeltin."));
			}

			return Ok(new SuccessDataResult<ReverseGeocodeCityDto>(resolved, "Şehir başarıyla tespit edildi."));
		}

		[HttpGet("genders")]
		public IActionResult GetGenders()
		{
			var result = new SuccessDataResult<List<Gender>>(ConstantData.Genders);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
