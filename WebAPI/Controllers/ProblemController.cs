using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Core.Utilities.Authorization;
using Core.Utilities.Hashing;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Filters;
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
        private readonly IHashidsService _hashids;
        private readonly IMediaAssetService _mediaAssetService;
        private readonly IProblemViewService _problemViewService;
        private readonly IProblemUpvoteService _problemUpvoteService;
        private readonly ICapabilityResolver _capabilityResolver;

        public ProblemController(
            IProblemService problemService,
            IWebHostEnvironment webHostEnvironment,
            IValidator<ProblemAddDto> validator,
            ISolutionService solutionService,
            IGeoLocationService geoLocationService,
            IInstitutionFeatureService institutionFeatureService,
            IHashidsService hashids,
            IMediaAssetService mediaAssetService,
            IProblemViewService problemViewService,
            IProblemUpvoteService problemUpvoteService,
            ICapabilityResolver capabilityResolver)
        {
            _problemService = problemService;
            _webHostEnvironment = webHostEnvironment;
            _validator = validator;
            _solutionService = solutionService;
            _geoLocationService = geoLocationService;
            _institutionFeatureService = institutionFeatureService;
            _hashids = hashids;
            _mediaAssetService = mediaAssetService;
            _problemViewService = problemViewService;
            _problemUpvoteService = problemUpvoteService;
            _capabilityResolver = capabilityResolver;
        }

        private int? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true) return null;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int uid) ? uid : null;
        }

        private void RecordView(int problemId, int? institutionId)
        {
            var userId = GetCurrentUserId();
            _problemViewService.RecordView(problemId, userId, institutionId ?? 1);
        }

        private void SetPublicId(ProblemDetailDto? dto)
        {
            if (dto != null) dto.PublicId = _hashids.Encode(dto.Id);
        }

        private void SetPublicIds(IEnumerable<ProblemDetailDto>? dtos)
        {
            if (dtos == null) return;
            foreach (var d in dtos) d.PublicId = _hashids.Encode(d.Id);
        }

        private void EnrichVideoUrls(ProblemDetailDto? dto)
        {
            if (dto == null) return;
            var videos = _mediaAssetService.GetByOwner("Problem", dto.Id).Data;
            if (videos != null && videos.Count > 0)
                dto.VideoUrls = videos.Select(v => $"/uploads/problems/{v.FileName}").ToList();
        }

        private void EnrichVisibility(ProblemDetailDto? dto)
        {
            if (dto == null) return;
            var iid = dto.InstitutionId;
            dto.ViewersVisibility      = _institutionFeatureService.GetFeatureValue(iid, "Social.ProblemViewersVisibility",     "admin_only");
            dto.UpvotersVisibility     = _institutionFeatureService.GetFeatureValue(iid, "Social.ProblemUpvotersVisibility",    "admin_and_owner");
            dto.ParticipantsVisibility = _institutionFeatureService.GetFeatureValue(iid, "Social.ProblemParticipantsVisibility","public");
            dto.SolutionVotersVisibility = _institutionFeatureService.GetFeatureValue(iid, "Social.SolutionVotersVisibility",   "admin_and_owner");
        }

        private bool CanSeeProblem(ProblemDetailDto dto)
        {
            if (!dto.IsHidden) return true;
            var uid = GetCurrentUserId();
            if (!uid.HasValue) return false;
            return _capabilityResolver.Allows(uid.Value, "moderation.problem_hide",
                new CapabilityRequestContext(InstitutionId: dto.InstitutionId));
        }

        [HttpGet("getbyid")]
        public IActionResult GetById(int id)
        {
            var result = _problemService.GetById(id);
            if (result.Success && result.Data != null && !CanSeeProblem(result.Data))
                return NotFound(new { success = false, message = "Sorun bulunamadı." });
            SetPublicId(result.Data);
            EnrichVideoUrls(result.Data);
            EnrichVisibility(result.Data);
            if (result.Success && result.Data != null)
                RecordView(id, result.Data.InstitutionId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbypublicid/{publicId}")]
        public IActionResult GetByPublicId(string publicId)
        {
            var id = _hashids.Decode(publicId);
            if (id == null) return NotFound(new { success = false, message = "Geçersiz ID." });
            var result = _problemService.GetById(id.Value);
            if (result.Success && result.Data != null && !CanSeeProblem(result.Data))
                return NotFound(new { success = false, message = "Sorun bulunamadı." });
            SetPublicId(result.Data);
            EnrichVideoUrls(result.Data);
            EnrichVisibility(result.Data);
            if (result.Success && result.Data != null)
                RecordView(id.Value, result.Data.InstitutionId);
            return result.Success && result.Data != null ? Ok(result) : NotFound(new { success = false, message = "Sorun bulunamadı." });
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
            SetPublicIds(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbysender")]
        public IActionResult GetBySender(int senderId)
        {
            var result = _problemService.GetBySender(senderId);
            SetPublicIds(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getishighlighted")]
        public IActionResult GetIsHighlighted()
        {
            var result = _problemService.GetIsHighlighted();
            SetPublicIds(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("add")]
        [RequireCapability("user.problem_create")]
        public async Task<IActionResult> Add([FromForm] ProblemAddDto problemAddDto, CancellationToken cancellationToken)
        {
            int institutionId = 1;
            int senderId = 0;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userIdClaim != null)
                    senderId = Convert.ToInt32(userIdClaim.Value);

                var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
                if (institutionClaim != null)
                    institutionId = Convert.ToInt32(institutionClaim.Value);
            }

            bool allowAnonymous = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowAnonymousReport", false);
            if (!allowAnonymous && senderId == 0)
                return Unauthorized("Kullanıcı girişi gereklidir.");

            var validationResult = _validator.Validate(problemAddDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var maxTitleLengthText = _institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxTitleLength", "200");
            if (!int.TryParse(maxTitleLengthText, out var maxTitleLength) || maxTitleLength <= 0)
                maxTitleLength = 200;

            if (!string.IsNullOrWhiteSpace(problemAddDto.Title) && problemAddDto.Title.Length > maxTitleLength)
                return BadRequest(new { success = false, message = $"Başlık {maxTitleLength} karakterden uzun olamaz." });

            if (_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireCategorySelection", false)
                && (problemAddDto.TopicIds == null || problemAddDto.TopicIds.Count == 0))
                return BadRequest(new { success = false, message = "Lütfen en az bir kategori seçin." });

            int maxProblemImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxProblemImageCount", "5"));
            if (problemAddDto.Images != null && problemAddDto.Images.Count > maxProblemImageCount)
                return BadRequest(new { success = false, message = $"En fazla {maxProblemImageCount} görsel yükleyebilirsiniz." });

            if (problemAddDto.Images != null && problemAddDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowImageUpload", true))
                return BadRequest(new { success = false, message = "Bu kurum için görsel yükleme özelliği devre dışı." });

            bool videoEnabled = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.EnableVideo", false);
            if (problemAddDto.Videos != null && problemAddDto.Videos.Count > 0 && !videoEnabled)
                return BadRequest(new { success = false, message = "Bu kurum için video yükleme özelliği devre dışı." });

            int finalCityCode = problemAddDto.CityCode;
            if (problemAddDto.Latitude.HasValue && problemAddDto.Longitude.HasValue)
            {
                if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireMapLocation", true))
                {
                    problemAddDto.Latitude = null;
                    problemAddDto.Longitude = null;
                }
                else
                {
                    var resolved = await _geoLocationService.ReverseGeocodeCityAsync(
                        problemAddDto.Latitude.Value, problemAddDto.Longitude.Value, cancellationToken);
                    if (resolved == null)
                        return BadRequest("Konumdan şehir tespit edilemedi. Lütfen pini doğru konuma taşıyın.");
                    finalCityCode = resolved.CityCode;
                }
            }

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");

            List<string> imagePaths = new();
            if (problemAddDto.Images != null && problemAddDto.Images.Count > 0)
            {
                foreach (var file in problemAddDto.Images)
                {
                    try
                    {
                        var path = Core.Utilities.Helpers.FileHelper.FileHelper.Add(file, uploadPath);
                        if (!string.IsNullOrEmpty(path)) imagePaths.Add(path);
                    }
                    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
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

            if (result.Success)
            {
                // Videolar — MediaAsset kaydı
                if (videoEnabled && problemAddDto.Videos != null && problemAddDto.Videos.Count > 0)
                {
                    foreach (var vFile in problemAddDto.Videos)
                    {
                        try
                        {
                            var vName = Core.Utilities.Helpers.FileHelper.FileHelper.AddVideo(vFile, uploadPath);
                            if (!string.IsNullOrEmpty(vName))
                            {
                                _mediaAssetService.Add(new MediaAsset
                                {
                                    OwnerType   = "Problem",
                                    OwnerId     = problem.Id,
                                    Kind        = "Video",
                                    FileName    = vName,
                                    ContentType = vFile.ContentType,
                                    SizeBytes   = vFile.Length,
                                    InstitutionId = institutionId
                                });
                            }
                        }
                        catch (InvalidOperationException ex) { /* Video hatası ana işlemi engellemesin */ _ = ex; }
                    }
                }

                // Anlık çözüm
                if (!string.IsNullOrWhiteSpace(problemAddDto.SolutionDescription))
                {
                    int maxSolutionImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxSolutionImageCount", "3"));
                    bool allowSolutionImageUpload = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowSolutionImageUpload", true);

                    List<string> solutionImagePaths = new();
                    string solUploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "solutions");

                    if (problemAddDto.SolutionImages != null && problemAddDto.SolutionImages.Count > 0
                        && allowSolutionImageUpload && problemAddDto.SolutionImages.Count <= maxSolutionImageCount)
                    {
                        foreach (var file in problemAddDto.SolutionImages)
                        {
                            try
                            {
                                var path = Core.Utilities.Helpers.FileHelper.FileHelper.Add(file, solUploadPath);
                                if (!string.IsNullOrEmpty(path)) solutionImagePaths.Add(path);
                            }
                            catch { /* Ignore */ }
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
                    var solResult = _solutionService.Add(solution);

                    // Anlık çözüm videoları
                    if (solResult.Success && videoEnabled && problemAddDto.SolutionVideos != null && problemAddDto.SolutionVideos.Count > 0)
                    {
                        foreach (var vFile in problemAddDto.SolutionVideos)
                        {
                            try
                            {
                                var vName = Core.Utilities.Helpers.FileHelper.FileHelper.AddVideo(vFile, solUploadPath);
                                if (!string.IsNullOrEmpty(vName))
                                {
                                    _mediaAssetService.Add(new MediaAsset
                                    {
                                        OwnerType   = "Solution",
                                        OwnerId     = solution.Id,
                                        Kind        = "Video",
                                        FileName    = vName,
                                        ContentType = vFile.ContentType,
                                        SizeBytes   = vFile.Length,
                                        InstitutionId = institutionId
                                    });
                                }
                            }
                            catch { /* Video hatası ana işlemi engellemesin */ }
                        }
                    }
                }
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("update")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Update([FromForm] ProblemUpdateDto updateDto, CancellationToken cancellationToken)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("Kullanıcı girişi gereklidir.");

            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
                institutionId = Convert.ToInt32(institutionClaim.Value);

            var maxTitleLengthText = _institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxTitleLength", "200");
            if (!int.TryParse(maxTitleLengthText, out var maxTitleLength) || maxTitleLength <= 0)
                maxTitleLength = 200;

            if (!string.IsNullOrWhiteSpace(updateDto.Title) && updateDto.Title.Length > maxTitleLength)
                return BadRequest(new { success = false, message = $"Başlık {maxTitleLength} karakterden uzun olamaz." });

            if (_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireCategorySelection", false)
                && (updateDto.TopicIds == null || updateDto.TopicIds.Count == 0))
                return BadRequest(new { success = false, message = "Lütfen en az bir kategori seçin." });

            int maxProblemImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxProblemImageCount", "5"));
            var existingImagesList = string.IsNullOrEmpty(updateDto.ImageUrls)
                ? new List<string>()
                : updateDto.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (updateDto.Images != null && (existingImagesList.Count + updateDto.Images.Count) > maxProblemImageCount)
                return BadRequest(new { success = false, message = $"Toplamda en fazla {maxProblemImageCount} görsel yükleyebilirsiniz." });

            if (updateDto.Images != null && updateDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowImageUpload", true))
                return BadRequest(new { success = false, message = "Bu kurum için görsel yükleme özelliği devre dışı." });

            bool videoEnabled = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.EnableVideo", false);
            if (updateDto.Videos != null && updateDto.Videos.Count > 0 && !videoEnabled)
                return BadRequest(new { success = false, message = "Bu kurum için video yükleme özelliği devre dışı." });

            string? finalAddress  = updateDto.ClearLocation ? null : updateDto.Address;
            double? finalLatitude = updateDto.ClearLocation ? null : updateDto.Latitude;
            double? finalLongitude= updateDto.ClearLocation ? null : updateDto.Longitude;

            if (finalLatitude.HasValue && finalLongitude.HasValue &&
                !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.RequireMapLocation", true))
            {
                finalLatitude = null; finalLongitude = null; finalAddress = null;
            }

            int finalCityCode = updateDto.CityCode;
            if (finalLatitude.HasValue && finalLongitude.HasValue)
            {
                var resolved = await _geoLocationService.ReverseGeocodeCityAsync(
                    finalLatitude.Value, finalLongitude.Value, cancellationToken);
                if (resolved == null)
                    return BadRequest("Konumdan şehir tespit edilemedi. Lütfen pini doğru konuma taşıyın.");
                finalCityCode = resolved.CityCode;
            }

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");

            // Kaldırılan görselleri bul ve sil
            var currentProblem = _problemService.GetById(updateDto.Id).Data;
            if (currentProblem?.ImageUrls != null)
            {
                var currentImages = currentProblem.ImageUrls; // List<string> zaten
                var removedImages = currentImages.Except(existingImagesList).ToList();
                foreach (var img in removedImages)
                    Core.Utilities.Helpers.FileHelper.FileHelper.Delete(img, uploadPath);
            }

            if (updateDto.Images != null && updateDto.Images.Count > 0)
            {
                foreach (var file in updateDto.Images)
                {
                    try
                    {
                        var path = Core.Utilities.Helpers.FileHelper.FileHelper.Add(file, uploadPath);
                        if (!string.IsNullOrEmpty(path)) existingImagesList.Add(path);
                    }
                    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
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

            var result = _problemService.Update(problem, updateDto.TopicIds);

            // Yeni videolar — MediaAsset kaydı
            if (result.Success && videoEnabled && updateDto.Videos != null && updateDto.Videos.Count > 0)
            {
                foreach (var vFile in updateDto.Videos)
                {
                    try
                    {
                        var vName = Core.Utilities.Helpers.FileHelper.FileHelper.AddVideo(vFile, uploadPath);
                        if (!string.IsNullOrEmpty(vName))
                        {
                            _mediaAssetService.Add(new MediaAsset
                            {
                                OwnerType   = "Problem",
                                OwnerId     = updateDto.Id,
                                Kind        = "Video",
                                FileName    = vName,
                                ContentType = vFile.ContentType,
                                SizeBytes   = vFile.Length,
                                InstitutionId = institutionId
                            });
                        }
                    }
                    catch { /* Video hatası ana işlemi engellemesin */ }
                }
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Delete(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("Kullanıcı girişi gereklidir.");

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
                    institutionId = Convert.ToInt32(claim.Value);
            }

            var result = _problemService.GetList(filterDto, institutionId);
            if (result.Success)
            {
                SetPublicIds(result.Data);
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

        [HttpGet("{id}/viewers")]
        public IActionResult GetViewers(int id)
        {
            var problem = _problemService.GetById(id);
            if (!problem.Success || problem.Data == null) return NotFound();

            var visValue = _institutionFeatureService.GetFeatureValue(problem.Data.InstitutionId, "Social.ProblemViewersVisibility", "admin_only");
            var level = VisibilityHelper.Parse(visValue);
            if (!VisibilityHelper.CanView(level, GetCurrentUserId(), problem.Data.SenderId, _capabilityResolver))
                return Forbid();

            var result = _problemViewService.GetViewers(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}/upvoters")]
        public IActionResult GetUpvoters(int id)
        {
            var problem = _problemService.GetById(id);
            if (!problem.Success || problem.Data == null) return NotFound();

            var visValue = _institutionFeatureService.GetFeatureValue(problem.Data.InstitutionId, "Social.ProblemUpvotersVisibility", "admin_and_owner");
            var level = VisibilityHelper.Parse(visValue);
            if (!VisibilityHelper.CanView(level, GetCurrentUserId(), problem.Data.SenderId, _capabilityResolver))
                return Forbid();

            var result = _problemUpvoteService.GetUpvoters(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/close")]
        [RequireCapability("moderation.problem_close")]
        public IActionResult CloseProblem(int id, [FromBody] CloseProblemDto dto)
        {
            var result = _problemService.CloseProblem(id, dto?.Reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/reopen")]
        [RequireCapability("moderation.problem_reopen")]
        public IActionResult ReopenProblem(int id)
        {
            var result = _problemService.ReopenProblem(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/togglehide")]
        [RequireCapability("moderation.problem_hide")]
        public IActionResult ToggleHide(int id)
        {
            var result = _problemService.ToggleHide(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}/participants")]
        public IActionResult GetParticipants(int id)
        {
            var problem = _problemService.GetById(id);
            if (!problem.Success || problem.Data == null) return NotFound();

            var visValue = _institutionFeatureService.GetFeatureValue(problem.Data.InstitutionId, "Social.ProblemParticipantsVisibility", "public");
            var level = VisibilityHelper.Parse(visValue);
            if (!VisibilityHelper.CanView(level, GetCurrentUserId(), problem.Data.SenderId, _capabilityResolver))
                return Forbid();

            var result = _problemService.GetParticipants(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
