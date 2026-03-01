using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstitutionController : Controller
    {
        private readonly IInstitutionService _institutionService;

        public InstitutionController(IInstitutionService institutionService)
        {
            _institutionService = institutionService;
        }

        [HttpGet("getall")]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            var result = _institutionService.GetAll();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbyid")]
        [AllowAnonymous]
        public IActionResult GetById(int id)
        {
            var result = _institutionService.GetById(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbydomain")]
        [AllowAnonymous]
        public IActionResult GetByDomain(string domain)
        {
            var result = _institutionService.GetByDomain(domain);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] Institution institution)
        {
            var result = _institutionService.Add(institution);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        public IActionResult Update([FromBody] Institution institution)
        {
            var result = _institutionService.Update(institution);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        public IActionResult Delete(int id)
        {
            var result = _institutionService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
