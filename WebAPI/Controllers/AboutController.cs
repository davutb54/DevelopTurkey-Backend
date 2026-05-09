using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AboutController : Controller
    {
        private readonly IAboutPageSectionService _aboutPageSectionService;

        public AboutController(IAboutPageSectionService aboutPageSectionService)
        {
            _aboutPageSectionService = aboutPageSectionService;
        }

        [AllowAnonymous]
        [HttpGet("active")]
        public IActionResult GetActiveSections()
        {
            var result = _aboutPageSectionService.GetActiveSections();
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
