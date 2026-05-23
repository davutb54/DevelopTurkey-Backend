using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SolutionController : Controller
	{
		private readonly ISolutionService _solutionService;
		private readonly IInstitutionFeatureService _institutionFeatureService;
        private readonly IWebHostEnvironment _webHostEnvironment;

		public SolutionController(ISolutionService solutionService, IInstitutionFeatureService institutionFeatureService, IWebHostEnvironment webHostEnvironment)
		{
			_solutionService = solutionService;
			_institutionFeatureService = institutionFeatureService;
            _webHostEnvironment = webHostEnvironment;
		}

        [HttpGet("getbyid")]
		public IActionResult GetById(int id)
		{
			var result = _solutionService.GetById(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getall")]
		public IActionResult GetAll()
		{
            int institutionId = 1;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
                if (claim != null)
                {
                    institutionId = Convert.ToInt32(claim.Value);
                }
            }

            var result = _solutionService.GetAll(institutionId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbyproblem")]
		public IActionResult GetByProblem(int problemId)
		{
			var result = _solutionService.GetByProblem(problemId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbysender")]
		public IActionResult GetBySender(int senderId)
		{
			var result = _solutionService.GetBySender(senderId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getishighlighted")]
		public IActionResult GetIsHighlighted()
		{
			var result = _solutionService.GetIsHighlighted();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("add")]
		[RequireCapability("user.solution_create")]
		public IActionResult Add([FromForm] SolutionAddDto solutionAddDto)
		{
			if (User.Identity == null || !User.Identity.IsAuthenticated)
			{
				return Unauthorized("Kullanıcı girişi gereklidir.");
			}

			// Feature: Content.MinSolutionLength
			int institutionId = 1;
			var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
			if (institutionClaim != null)
			{
				institutionId = Convert.ToInt32(institutionClaim.Value);
			}

			int minSolutionLength = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MinSolutionLength", "50"));
			if (!string.IsNullOrEmpty(solutionAddDto.Description) && solutionAddDto.Description.Trim().Length < minSolutionLength)
			{
				return BadRequest(new { success = false, message = $"Çözüm açıklaması en az {minSolutionLength} karakter olmalıdır." });
			}

            // Feature: Çözüm görsel yükleme kontrolü ve limit kontrolü
            int maxSolutionImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxSolutionImageCount", "3"));
            if (solutionAddDto.Images != null && solutionAddDto.Images.Count > maxSolutionImageCount)
                return BadRequest(new { success = false, message = $"Çözüm için en fazla {maxSolutionImageCount} görsel yükleyebilirsiniz." });

            if (solutionAddDto.Images != null && solutionAddDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowSolutionImageUpload", true))
                return BadRequest(new { success = false, message = "Çözümler için görsel yükleme özelliği devre dışı." });

            List<string> imagePaths = new List<string>();
            if (solutionAddDto.Images != null && solutionAddDto.Images.Count > 0)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "solutions");
                foreach (var file in solutionAddDto.Images)
                {
                    try
                    {
                        var path = Core.Utilities.Helpers.FileHelper.FileHelper.Add(file, uploadPath);
                        if (!string.IsNullOrEmpty(path)) imagePaths.Add(path);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
            }
            string? finalImageUrls = imagePaths.Count > 0 ? string.Join(",", imagePaths) : null;

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            int senderId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            var solution = new Solution
            {
                SenderId = senderId,
                ProblemId = solutionAddDto.ProblemId,
                Title = solutionAddDto.Title,
                Description = solutionAddDto.Description,
                ImageUrls = finalImageUrls,
                InstitutionId = institutionId,
                SendDate = DateTime.Now,
                IsHighlighted = false,
                IsReported = false,
                IsDeleted = false,
                ExpertApprovalStatus = 0
            };

			var result = _solutionService.Add(solution);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
		[Authorize]
		public IActionResult Update([FromForm] SolutionUpdateDto solutionUpdateDto)
		{
			if (User.Identity == null || !User.Identity.IsAuthenticated)
			{
				return Unauthorized("Kullanıcı girişi gereklidir.");
			}

            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
            {
                institutionId = Convert.ToInt32(institutionClaim.Value);
            }

            int minSolutionLength = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MinSolutionLength", "50"));
            if (!string.IsNullOrEmpty(solutionUpdateDto.Description) && solutionUpdateDto.Description.Trim().Length < minSolutionLength)
            {
                return BadRequest(new { success = false, message = $"Çözüm açıklaması en az {minSolutionLength} karakter olmalıdır." });
            }

            // Feature: Görsel yükleme kontrolü ve limit kontrolü
            int maxSolutionImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxSolutionImageCount", "3"));
            var existingImagesList = string.IsNullOrEmpty(solutionUpdateDto.ImageUrls)
                ? new List<string>()
                : solutionUpdateDto.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (solutionUpdateDto.Images != null && (existingImagesList.Count + solutionUpdateDto.Images.Count) > maxSolutionImageCount)
                return BadRequest(new { success = false, message = $"Toplamda en fazla {maxSolutionImageCount} görsel yükleyebilirsiniz." });

            if (solutionUpdateDto.Images != null && solutionUpdateDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowSolutionImageUpload", true))
                return BadRequest(new { success = false, message = "Çözümler için görsel yükleme özelliği devre dışı." });

            if (solutionUpdateDto.Images != null && solutionUpdateDto.Images.Count > 0)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "solutions");
                foreach (var file in solutionUpdateDto.Images)
                {
                    try
                    {
                        var path = Core.Utilities.Helpers.FileHelper.FileHelper.Add(file, uploadPath);
                        if (!string.IsNullOrEmpty(path)) existingImagesList.Add(path);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
            }
            string? finalImageUrls = existingImagesList.Count > 0 ? string.Join(",", existingImagesList) : null;

            var solution = new Solution
            {
                Id = solutionUpdateDto.Id,
                SenderId = solutionUpdateDto.SenderId,
                ProblemId = solutionUpdateDto.ProblemId,
                Title = solutionUpdateDto.Title,
                Description = solutionUpdateDto.Description,
                ImageUrls = finalImageUrls,
                SendDate = solutionUpdateDto.SendDate,
                IsHighlighted = solutionUpdateDto.IsHighlighted,
                IsReported = solutionUpdateDto.IsReported,
                IsDeleted = solutionUpdateDto.IsDeleted,
                ExpertApprovalStatus = solutionUpdateDto.ExpertApprovalStatus,
                InstitutionId = solutionUpdateDto.InstitutionId
            };

            var result = _solutionService.Update(solution);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpDelete("delete")]
		[Authorize]
		public IActionResult Delete(int id)
		{
			if (User.Identity == null || !User.Identity.IsAuthenticated)
			{
				return Unauthorized("Kullanıcı girişi gereklidir.");
			}

			var result = _solutionService.Delete(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
