using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SolutionController : Controller
	{
		private readonly ISolutionService _solutionService;

		public SolutionController(ISolutionService solutionService)
		{
			_solutionService = solutionService;
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
		[Authorize]
		public IActionResult Add(Solution solution)
		{
			if (User.Identity == null || !User.Identity.IsAuthenticated)
			{
				return Unauthorized("Kullanıcı girişi gereklidir.");
			}

			var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
			if (userIdClaim == null)
			{
				return Unauthorized("Geçersiz token.");
			}

			
			solution.SenderId = Convert.ToInt32(userIdClaim.Value);
			solution.SendDate = DateTime.Now;
			solution.IsHighlighted = false;
			solution.IsReported = false;
			solution.IsDeleted = false;
			solution.ExpertApprovalStatus = 0;

			var result = _solutionService.Add(solution);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
		[Authorize]
		public IActionResult Update(Solution solution)
		{
			if (User.Identity == null || !User.Identity.IsAuthenticated)
			{
				return Unauthorized("Kullanıcı girişi gereklidir.");
			}

			var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
			if (userIdClaim == null)
			{
				return Unauthorized("Geçersiz token.");
			}

			int currentUserId = Convert.ToInt32(userIdClaim.Value);

			
			var isAdminClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Admin");
			bool isAdmin = isAdminClaim != null;

			var existingSolution = _solutionService.GetById(solution.Id);
			if (!existingSolution.Success || existingSolution.Data == null)
			{
				return NotFound("Çözüm bulunamadı.");
			}

			
			if (!isAdmin && existingSolution.Data.SenderId != currentUserId)
			{
				return Forbid("Bu çözümü güncelleme yetkiniz yok.");
			}

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

			var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
			if (userIdClaim == null)
			{
				return Unauthorized("Geçersiz token.");
			}

			int currentUserId = Convert.ToInt32(userIdClaim.Value);

			
			var isAdminClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Admin");
			bool isAdmin = isAdminClaim != null;

			var existingSolution = _solutionService.GetById(id);
			if (!existingSolution.Success || existingSolution.Data == null)
			{
				return NotFound("Çözüm bulunamadı.");
			}

			if (!isAdmin && existingSolution.Data.SenderId != currentUserId)
			{
				return Forbid("Bu çözümü silme yetkiniz yok.");
			}

			var result = _solutionService.Delete(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
