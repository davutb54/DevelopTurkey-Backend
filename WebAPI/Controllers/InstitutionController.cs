using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Filters;

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

        /// <summary>
        /// Tüm kurumları listeler.
        /// Global admin.institution_read → tümünü döner.
        /// Scoped (kuruma bağlı) admin → yalnız kendi kurumunu döner.
        /// </summary>
        [HttpGet("getall")]
        [RequireCapability("admin.institution_read")]
        public IActionResult GetAll()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                return Unauthorized();

            var resolver      = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var clientContext = HttpContext.RequestServices.GetRequiredService<IClientContext>();

            // Global admin check (no InstitutionId scope = global)
            if (resolver.Allows(currentUserId, "admin.institution_read", ctx: null))
            {
                var all = _institutionService.GetAll();
                return all.Success ? Ok(all) : BadRequest(all);
            }

            // Scoped admin — return only their own institution
            var institutionId = clientContext.GetInstitutionId();
            if (!institutionId.HasValue)
                return Forbid();

            var own = _institutionService.GetById(institutionId.Value);
            if (!own.Success || own.Data == null)
                return BadRequest(own);

            return Ok(new { success = true, data = new List<Institution> { own.Data } });
        }

        /// <summary>
        /// Anonim erişim — yalnız branding bilgilerini döner.
        /// </summary>
        [HttpGet("getbydomain")]
        [AllowAnonymous]
        public IActionResult GetByDomain(string domain)
        {
            var result = _institutionService.GetPublicInfo(domain);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Subdomain tabanlı kurum tespiti — anonim, yalnız branding bilgilerini döner.
        /// Örnek: GET /api/institution/getbysubdomain?slug=kurum1
        /// </summary>
        [HttpGet("getbysubdomain")]
        [AllowAnonymous]
        public IActionResult GetBySubdomain(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest(new { success = false, message = "slug parametresi gereklidir." });

            var result = _institutionService.GetPublicInfoBySubdomain(slug.Trim().ToLowerInvariant());
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("getbyid")]
        [AllowAnonymous]
        public IActionResult GetById(int id)
        {
            var result = _institutionService.GetById(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("add")]
        [RequireCapability("admin.institution_create")]
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
                Subtitle = dto.Subtitle,
                Domain = dto.Domain,
                Subdomain = string.IsNullOrWhiteSpace(dto.Subdomain) ? null : dto.Subdomain.Trim().ToLowerInvariant(),
                PrimaryColor = dto.PrimaryColor,
                Status = dto.Status,
                LogoUrl = logoUrl,
                CustomFieldsJson = dto.CustomFieldsJson,
                CustomHierarchyLabel = dto.CustomHierarchyLabel,
                CustomHierarchyJson = dto.CustomHierarchyJson
            };
            var result = _institutionService.Add(institution);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        [RequireCapability("admin.institution_update")]
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
            existingInst.Subtitle = dto.Subtitle;
            existingInst.Domain = dto.Domain;
            existingInst.Subdomain = string.IsNullOrWhiteSpace(dto.Subdomain) ? null : dto.Subdomain.Trim().ToLowerInvariant();
            existingInst.PrimaryColor = dto.PrimaryColor;
            existingInst.Status = dto.Status;
            existingInst.LogoUrl = logoUrl;
            existingInst.CustomFieldsJson = dto.CustomFieldsJson;
            existingInst.CustomHierarchyLabel = dto.CustomHierarchyLabel;
            existingInst.CustomHierarchyJson = dto.CustomHierarchyJson;

            var result = _institutionService.Update(existingInst);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        [RequireCapability("admin.institution_deactivate")]
        public IActionResult Delete(int id)
        {
            var result = _institutionService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
