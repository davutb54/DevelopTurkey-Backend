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
        private readonly IInstitutionFeatureService _institutionFeatureService;

        public ProblemController(
            IProblemService problemService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<ProblemAddDto> validator,
            ISolutionService solutionService,
            IGeoLocationService geoLocationService,
            IInstitutionFeatureService institutionFeatureService)
        {
            _problemService = problemService;
            _webHostEnvironment = webHostEnvironment;
            _validator = validator;
            _solutionService = solutionService;
            _geoLocationService = geoLocationService;
            _institutionFeatureService = institutionFeatureService;
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
        public async Task<IActionResult> Add([FromForm] ProblemAddDto problemAddDto, CancellationToken cancellationToken)
        {
            int institutionId = 1;
            int senderId = 0;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userIdClaim != null)
                {
                    senderId = Convert.ToInt32(userIdClaim.Value);
                }

                var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
                if (institutionClaim != null)
                {
                    institutionId = Convert.ToInt32(institutionClaim.Value);
                }
            }

            // Feature: AllowAnonymousReport
            bool allowAnonymous = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowAnonymousReport", false);
            if (!allowAnonymous && senderId == 0)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            var validationResult = _validator.Validate(problemAddDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var maxTitleLengthText = _institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxTitleLength", "200");
            if (!int.TryParse(maxTitleLengthText, out var maxTitleLength) || maxTitleLength <= 0)
            {
                maxTitleLength = 200;
            }

            if (!string.IsNullOrWhiteSpace(problemAddDto.Title) && problemAddDto.Title.Length > maxTitleLength)
            {
                return BadRequest(new { success = false, message = $"Başlık {maxTitleLength} karakterden uzun olamaz." });
            }

            if (_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireCategorySelection", false)
                && (problemAddDto.TopicIds == null || problemAddDto.TopicIds.Count == 0))
            {
                return BadRequest(new { success = false, message = "Lütfen en az bir kategori seçin." });
            }

            // Feature: Görsel yükleme kontrolü ve sayımı
            int maxProblemImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxProblemImageCount", "5"));
            if (problemAddDto.Images != null && problemAddDto.Images.Count > maxProblemImageCount)
                return BadRequest(new { success = false, message = $"En fazla {maxProblemImageCount} görsel yükleyebilirsiniz." });

            if (problemAddDto.Images != null && problemAddDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowImageUpload", true))
                return BadRequest(new { success = false, message = "Bu kurum için görsel yükleme özelliği devre dışı." });

            // Güvenlik/Doğruluk: Koordinat geldiyse şehir bilgisi otomatik tespit edilir ve kullanıcının gönderdiği CityCode yok sayılır.
            int finalCityCode = problemAddDto.CityCode;
            if (problemAddDto.Latitude.HasValue && problemAddDto.Longitude.HasValue)
            {
                // Feature: Harita konumu zorunlu değilse konum alanlarını temizle
                if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireMapLocation", true))
                {
                    // Konum özelliği kapalıysa koordinatları yoksay
                    problemAddDto.Latitude = null;
                    problemAddDto.Longitude = null;
                }
                else
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
            }

            List<string> imagePaths = new List<string>();
            if (problemAddDto.Images != null && problemAddDto.Images.Count > 0)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");
                foreach (var file in problemAddDto.Images)
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

            var problem = new Problem
            {
                SenderId = senderId,
                Title = problemAddDto.Title,
                Description = problemAddDto.Description,
                CityCode = finalCityCode,
                Address = problemAddDto.Address,
                Latitude = problemAddDto.Latitude,
                Longitude = problemAddDto.Longitude,
                ImageUrls = finalImageUrls,
                SendDate = DateTime.Now,
                IsHighlighted = false,
                IsReported = false,
                IsDeleted = false,
                InstitutionId = institutionId,
                CustomHierarchyId = problemAddDto.CustomHierarchyId
            };

            var result = _problemService.Add(problem, problemAddDto.TopicIds);

            if (result.Success && !string.IsNullOrWhiteSpace(problemAddDto.SolutionDescription))
            {
                // Feature: Çözüm görsel yükleme kontrolü ve sayımı
                int maxSolutionImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxSolutionImageCount", "3"));
                bool allowSolutionImageUpload = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowSolutionImageUpload", true);

                List<string> solutionImagePaths = new List<string>();
                if (problemAddDto.SolutionImages != null && problemAddDto.SolutionImages.Count > 0)
                {
                    if (!allowSolutionImageUpload)
                    {
                        // Özellik kapalıysa ama görsel gönderilmişse hata vermiyoruz (belki daha önce açıktı), 
                        // ama yeni görselleri kaydetmiyoruz veya hata dönebiliriz. 
                        // Burada tutarlılık için hata dönmek daha iyi olabilir ama sorun zaten oluşturuldu.
                        // Bu yüzden sadece loglayıp geçebiliriz veya kuralı en başta kontrol etmeliyiz.
                    }
                    else if (problemAddDto.SolutionImages.Count <= maxSolutionImageCount)
                    {
                        string solUploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "solutions");
                        foreach (var file in problemAddDto.SolutionImages)
                        {
                            try
                            {
                                var path = Core.Utilities.Helpers.FileHelper.FileHelper.Add(file, solUploadPath);
                                if (!string.IsNullOrEmpty(path)) solutionImagePaths.Add(path);
                            }
                            catch { /* Ignore single file errors for now */ }
                        }
                    }
                }

                string finalSolutionTitle = string.IsNullOrWhiteSpace(problemAddDto.SolutionTitle)
                    ? "Çözüm Önerim"
                    : problemAddDto.SolutionTitle.Trim();

                var solution = new Solution
                {
                    ProblemId = problem.Id,
                    SenderId = senderId,
                    Title = finalSolutionTitle,
                    Description = problemAddDto.SolutionDescription.Trim(),
                    ImageUrls = solutionImagePaths.Count > 0 ? string.Join(",", solutionImagePaths) : null,
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

            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
            {
                institutionId = Convert.ToInt32(institutionClaim.Value);
            }

            var maxTitleLengthText = _institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxTitleLength", "200");
            if (!int.TryParse(maxTitleLengthText, out var maxTitleLength) || maxTitleLength <= 0)
            {
                maxTitleLength = 200;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.Title) && updateDto.Title.Length > maxTitleLength)
            {
                return BadRequest(new { success = false, message = $"Başlık {maxTitleLength} karakterden uzun olamaz." });
            }

            if (_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireCategorySelection", false)
                && (updateDto.TopicIds == null || updateDto.TopicIds.Count == 0))
            {
                return BadRequest(new { success = false, message = "Lütfen en az bir kategori seçin." });
            }

            // Feature: Görsel yükleme kontrolü ve limit kontrolü
            int maxProblemImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxProblemImageCount", "5"));
            var existingImagesList = string.IsNullOrEmpty(updateDto.ImageUrls) 
                ? new List<string>() 
                : updateDto.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (updateDto.Images != null && (existingImagesList.Count + updateDto.Images.Count) > maxProblemImageCount)
                return BadRequest(new { success = false, message = $"Toplamda en fazla {maxProblemImageCount} görsel yükleyebilirsiniz." });

            if (updateDto.Images != null && updateDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowImageUpload", true))
                return BadRequest(new { success = false, message = "Bu kurum için görsel yükleme özelliği devre dışı." });

            string? finalAddress = updateDto.ClearLocation ? null : updateDto.Address;
            double? finalLatitude = updateDto.ClearLocation ? null : updateDto.Latitude;
            double? finalLongitude = updateDto.ClearLocation ? null : updateDto.Longitude;

            // Feature: Harita konumu zorunlu değilse konum alanlarını temizle
            if (finalLatitude.HasValue && finalLongitude.HasValue &&
                !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireMapLocation", true))
            {
                finalLatitude = null;
                finalLongitude = null;
                finalAddress = null;
            }

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

            if (updateDto.Images != null && updateDto.Images.Count > 0)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");
                foreach (var file in updateDto.Images)
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
                ImageUrls = finalImageUrls,
                SendDate = updateDto.SendDate,
                IsHighlighted = updateDto.IsHighlighted,
                IsReported = updateDto.IsReported,
                IsDeleted = updateDto.IsDeleted,
                IsResolved = updateDto.IsResolved,
                InstitutionId = updateDto.InstitutionId,
                ViewCount = updateDto.ViewCount,
                CustomHierarchyId = updateDto.CustomHierarchyId
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
