using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemController : Controller
    {
        private readonly IProblemService _problemService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IValidator<ProblemAddDto> _validator;
        private readonly ISolutionService _solutionService;
        private readonly IGeoLocationService _geoLocationService;

        public ProblemController(
            IProblemService problemService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<ProblemAddDto> validator,
            ISolutionService solutionService,
            IGeoLocationService geoLocationService)
        {
            _problemService = problemService;
            _webHostEnvironment = webHostEnvironment;
            _validator = validator;
            _solutionService = solutionService;
            _geoLocationService = geoLocationService;
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
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Add([FromForm] ProblemAddDto problemAddDto, CancellationToken cancellationToken)
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

            // Güvenlik/Doğruluk: Koordinat geldiyse şehir bilgisi otomatik tespit edilir ve kullanıcının gönderdiği CityCode yok sayılır.
            int finalCityCode = problemAddDto.CityCode;
            if (problemAddDto.Latitude.HasValue && problemAddDto.Longitude.HasValue)
            {
                var resolved = await _geoLocationService.ReverseGeocodeCityAsync(
                    problemAddDto.Latitude.Value,
                    problemAddDto.Longitude.Value,
                    cancellationToken);

                if (resolved == null)
                {
                    return BadRequest("Konumdan şehir tespit edilemedi. Lütfen pini doğru konuma taşıyın.");
                }

                finalCityCode = resolved.CityCode;
            }

            string? imagePath = null;
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
                CityCode = finalCityCode,
                Address = problemAddDto.Address,
                Latitude = problemAddDto.Latitude,
                Longitude = problemAddDto.Longitude,
                ImageUrl = imagePath,
                SendDate = DateTime.Now,
                IsHighlighted = false,
                IsReported = false,
                IsDeleted = false,
                InstitutionId = institutionId
            };

            var result = _problemService.Add(problem, problemAddDto.TopicIds);

            if (result.Success && !string.IsNullOrWhiteSpace(problemAddDto.SolutionDescription))
            {
                string finalSolutionTitle = string.IsNullOrWhiteSpace(problemAddDto.SolutionTitle)
                    ? "Çözüm Önerim"
                    : problemAddDto.SolutionTitle.Trim();

                var solution = new Solution
                {
                    ProblemId = problem.Id,
                    SenderId = senderId,
                    Title = finalSolutionTitle,
                    Description = problemAddDto.SolutionDescription.Trim(),
                    SendDate = DateTime.Now,
                    IsHighlighted = false,
                    IsReported = false,
                    IsDeleted = false,
                    ExpertApprovalStatus = 0
                };
                _solutionService.Add(solution);
            }
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("update")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Update([FromForm] ProblemUpdateDto updateDto, CancellationToken cancellationToken)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            string? finalAddress = updateDto.ClearLocation ? null : updateDto.Address;
            double? finalLatitude = updateDto.ClearLocation ? null : updateDto.Latitude;
            double? finalLongitude = updateDto.ClearLocation ? null : updateDto.Longitude;

            // Koordinat geldiyse şehir bilgisi otomatik tespit edilir.
            int finalCityCode = updateDto.CityCode;
            if (finalLatitude.HasValue && finalLongitude.HasValue)
            {
                var resolved = await _geoLocationService.ReverseGeocodeCityAsync(
                    finalLatitude.Value,
                    finalLongitude.Value,
                    cancellationToken);

                if (resolved == null)
                {
                    return BadRequest("Konumdan şehir tespit edilemedi. Lütfen pini doğru konuma taşıyın.");
                }

                finalCityCode = resolved.CityCode;
            }

            string? imagePath = updateDto.ImageUrl;
            if (updateDto.Image != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");

                try
                {
                    imagePath = Core.Utilities.Helpers.FileHelper.FileHelper.Add(updateDto.Image, uploadPath);
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
                Id = updateDto.Id,
                SenderId = updateDto.SenderId,
                Title = updateDto.Title,
                Description = updateDto.Description,
                CityCode = finalCityCode,
                Address = finalAddress,
                Latitude = finalLatitude,
                Longitude = finalLongitude,
                ImageUrl = imagePath,
                SendDate = updateDto.SendDate,
                IsHighlighted = updateDto.IsHighlighted,
                IsReported = updateDto.IsReported,
                IsDeleted = updateDto.IsDeleted,
                IsResolved = updateDto.IsResolved,
                InstitutionId = updateDto.InstitutionId,
                ViewCount = updateDto.ViewCount
            };

            var result = _problemService.Update(problem,updateDto.TopicIds);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Delete(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
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

            var result = _problemService.GetList(filterDto, institutionId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("incrementview")]
        public IActionResult IncrementView(int id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = _problemService.IncrementView(id, ipAddress);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
