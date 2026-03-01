using Business.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstitutionController : Controller
    {
        private readonly IInstitutionService _institutionService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public InstitutionController(IInstitutionService institutionService, IWebHostEnvironment webHostEnvironment)
        {
            _institutionService = institutionService;
            _webHostEnvironment = webHostEnvironment;
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
        [Authorize(Roles = "Admin")]
        public IActionResult Add([FromForm] InstitutionAddDto dto)
        {
            string logoUrl = null;
            if (dto.Logo != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "institutions");
                try
                {
                    string fileName = Core.Utilities.Helpers.FileHelper.FileHelper.Add(dto.Logo, uploadPath);
                    logoUrl = "/uploads/institutions/" + fileName;
                }
                catch (Exception ex) { return BadRequest(ex.Message); }
            }

            var institution = new Institution
            {
                Name = dto.Name,
                Domain = dto.Domain,
                PrimaryColor = dto.PrimaryColor,
                Status = dto.Status,
                LogoUrl = logoUrl
            };
            var result = _institutionService.Add(institution);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        [Authorize(Roles = "Admin")]
        public IActionResult Update([FromForm] InstitutionUpdateDto dto)
        {
            var existingInst = _institutionService.GetById(dto.Id).Data;
            if (existingInst == null) return BadRequest("Kurum bulunamadı");

            string logoUrl = dto.ExistingLogoUrl ?? existingInst.LogoUrl;

            if (dto.Logo != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "institutions");
                try
                {
                    string fileName = Core.Utilities.Helpers.FileHelper.FileHelper.Add(dto.Logo, uploadPath);
                    logoUrl = "/uploads/institutions/" + fileName;
                }
                catch (Exception ex) { return BadRequest(ex.Message); }
            }

            existingInst.Name = dto.Name;
            existingInst.Domain = dto.Domain;
            existingInst.PrimaryColor = dto.PrimaryColor;
            existingInst.Status = dto.Status;
            existingInst.LogoUrl = logoUrl;

            var result = _institutionService.Update(existingInst);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var result = _institutionService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
