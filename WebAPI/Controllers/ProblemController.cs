using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemController : Controller
    {
        private readonly IProblemService _problemService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IValidator<ProblemAddDto> _validator;

        public ProblemController(
            IProblemService problemService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<ProblemAddDto> validator)
        {
            _problemService = problemService;
            _webHostEnvironment = webHostEnvironment;
            _validator = validator;
        }

        [HttpGet("getbyid")]
        public IActionResult GetById(int id)
        {
            var result = _problemService.GetById(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            var result = _problemService.GetAll();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbytopic")]
        public IActionResult GetByTopic(int topicId)
        {
            var result = _problemService.GetByTopic(topicId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbysender")]
        public IActionResult GetBySender(int senderId)
        {
            var result = _problemService.GetBySender(senderId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getishighlighted")]
        public IActionResult GetIsHighlighted()
        {
            var result = _problemService.GetIsHighlighted();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("add")]
        public IActionResult Add([FromForm] ProblemAddDto problemAddDto)
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

            int senderId = Convert.ToInt32(userIdClaim.Value);

            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
            {
                institutionId = Convert.ToInt32(institutionClaim.Value);
            }

            var validationResult = _validator.Validate(problemAddDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            string imagePath = null;
            if (problemAddDto.Image != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");

                try
                {
                    imagePath = Core.Utilities.Helpers.FileHelper.FileHelper.Add(problemAddDto.Image, uploadPath);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch (Exception)
                {
                    return StatusCode(500, "Dosya yüklenirken bir hata oluştu.");
                }
            }

            var problem = new Problem
            {
                SenderId = senderId,
                Title = problemAddDto.Title,
                Description = problemAddDto.Description,
                CityCode = problemAddDto.CityCode,
                TopicId = problemAddDto.TopicId,
                ImageUrl = imagePath,
                SendDate = DateTime.Now,
                IsHighlighted = false,
                IsReported = false,
                IsDeleted = false,
                InstitutionId = institutionId
            };

            var result = _problemService.Add(problem);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("update")]
        public IActionResult Update(Problem problem)
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

            
            var existingProblem = _problemService.GetById(problem.Id);
            if (!existingProblem.Success || existingProblem.Data == null)
            {
                return NotFound("Sorun bulunamadı.");
            }

            
            if (!isAdmin && existingProblem.Data.SenderId != currentUserId)
            {
                return Forbid("Bu sorunu güncelleme yetkiniz yok.");
            }

            var result = _problemService.Update(problem);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
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

            
            var existingProblem = _problemService.GetById(id);
            if (!existingProblem.Success || existingProblem.Data == null)
            {
                return NotFound("Sorun bulunamadı.");
            }

            
            if (!isAdmin && existingProblem.Data.SenderId != currentUserId)
            {
                return Forbid("Bu sorunu silme yetkiniz yok.");
            }

            var result = _problemService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getlist")]
        public IActionResult GetList([FromQuery] ProblemFilterDto filterDto)
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

            var result = _problemService.GetList(filterDto,institutionId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("incrementview")]
        public IActionResult IncrementView(int id)
        {
            var result = _problemService.IncrementView(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
