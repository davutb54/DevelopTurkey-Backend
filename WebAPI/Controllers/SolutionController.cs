using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Core.Utilities.Authorization;
using Core.Utilities.Hashing;
using Core.Utilities.Results;
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
    public class SolutionController : Controller
    {
        private readonly ISolutionService _solutionService;
        private readonly IInstitutionFeatureService _institutionFeatureService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHashidsService _hashids;
        private readonly IMediaAssetService _mediaAssetService;
        private readonly ISolutionVoteService _solutionVoteService;
        private readonly ICapabilityResolver _capabilityResolver;

        public SolutionController(
            ISolutionService solutionService,
            IInstitutionFeatureService institutionFeatureService,
            IWebHostEnvironment webHostEnvironment,
            IHashidsService hashids,
            IMediaAssetService mediaAssetService,
            ISolutionVoteService solutionVoteService,
            ICapabilityResolver capabilityResolver)
        {
            _solutionService = solutionService;
            _institutionFeatureService = institutionFeatureService;
            _webHostEnvironment = webHostEnvironment;
            _hashids = hashids;
            _mediaAssetService = mediaAssetService;
            _solutionVoteService = solutionVoteService;
            _capabilityResolver = capabilityResolver;
        }

        private int? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true) return null;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int uid) ? uid : null;
        }

        private void SetPublicId(SolutionDetailDto? dto)
        {
            if (dto != null) dto.PublicId = _hashids.Encode(dto.Id);
        }

        private void SetPublicIds(IEnumerable<SolutionDetailDto>? dtos)
        {
            if (dtos == null) return;
            foreach (var d in dtos)
            {
                d.PublicId        = _hashids.Encode(d.Id);
                d.ProblemPublicId = _hashids.Encode(d.ProblemId);
            }
        }

        private void EnrichVideoUrls(SolutionDetailDto? dto)
        {
            if (dto == null) return;
            var videos = _mediaAssetService.GetByOwner("Solution", dto.Id).Data;
            if (videos != null && videos.Count > 0)
                dto.VideoUrls = videos.Select(v => $"/uploads/solutions/{v.FileName}").ToList();
        }

        private void EnrichVideoUrlsBatch(IEnumerable<SolutionDetailDto>? dtos)
        {
            if (dtos == null) return;
            var ids = dtos.Select(d => d.Id).ToList();
            if (ids.Count == 0) return;
            var assets = _mediaAssetService.GetByOwners("Solution", ids).Data;
            if (assets == null || assets.Count == 0) return;
            var map = assets.GroupBy(a => a.OwnerId)
                            .ToDictionary(g => g.Key, g => g.Select(a => $"/uploads/solutions/{a.FileName}").ToList());
            foreach (var d in dtos)
            {
                if (map.TryGetValue(d.Id, out var urls))
                    d.VideoUrls = urls;
            }
        }

        [HttpGet("getbyid")]
        public IActionResult GetById(int id)
        {
            var result = _solutionService.GetById(id);
            // GetById returns Solution entity; video enrichment not applicable here
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbypublicid/{publicId}")]
        public IActionResult GetByPublicId(string publicId)
        {
            var id = _hashids.Decode(publicId);
            if (id == null) return NotFound(new { success = false, message = "Geçersiz ID." });
            var result = _solutionService.GetById(id.Value);
            return result.Success && result.Data != null ? Ok(result) : NotFound(new { success = false, message = "Çözüm bulunamadı." });
        }

        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            int institutionId = 1;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
                if (claim != null) institutionId = Convert.ToInt32(claim.Value);
            }
            var result = _solutionService.GetAll(institutionId);
            SetPublicIds(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbyproblem")]
        public IActionResult GetByProblem(int problemId)
        {
            var result = _solutionService.GetByProblem(problemId);
            SetPublicIds(result.Data);
            EnrichVideoUrlsBatch(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getbysender")]
        public IActionResult GetBySender(int senderId)
        {
            var result = _solutionService.GetBySender(senderId);
            SetPublicIds(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getishighlighted")]
        public IActionResult GetIsHighlighted()
        {
            var result = _solutionService.GetIsHighlighted();
            SetPublicIds(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("add")]
        [RequireCapability("user.solution_create")]
        public IActionResult Add([FromForm] SolutionAddDto solutionAddDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("Kullanıcı girişi gereklidir.");

            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
                institutionId = Convert.ToInt32(institutionClaim.Value);

            int minSolutionLength = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MinSolutionLength", "50"));
            if (!string.IsNullOrEmpty(solutionAddDto.Description) && solutionAddDto.Description.Trim().Length < minSolutionLength)
                return BadRequest(new { success = false, message = $"Çözüm açıklaması en az {minSolutionLength} karakter olmalıdır." });

            int maxSolutionImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxSolutionImageCount", "3"));
            if (solutionAddDto.Images != null && solutionAddDto.Images.Count > maxSolutionImageCount)
                return BadRequest(new { success = false, message = $"Çözüm için en fazla {maxSolutionImageCount} görsel yükleyebilirsiniz." });

            if (solutionAddDto.Images != null && solutionAddDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowSolutionImageUpload", true))
                return BadRequest(new { success = false, message = "Çözümler için görsel yükleme özelliği devre dışı." });

            bool videoEnabled = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.EnableVideo", false);
            if (solutionAddDto.Videos != null && solutionAddDto.Videos.Count > 0 && !videoEnabled)
                return BadRequest(new { success = false, message = "Bu kurum için video yükleme özelliği devre dışı." });

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "solutions");

            List<string> imagePaths = new();
            if (solutionAddDto.Images != null && solutionAddDto.Images.Count > 0)
            {
                foreach (var file in solutionAddDto.Images)
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

            // Videolar — MediaAsset kaydı
            if (result.Success && videoEnabled && solutionAddDto.Videos != null && solutionAddDto.Videos.Count > 0)
            {
                foreach (var vFile in solutionAddDto.Videos)
                {
                    try
                    {
                        var vName = Core.Utilities.Helpers.FileHelper.FileHelper.AddVideo(vFile, uploadPath);
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

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("update")]
        [Authorize]
        public IActionResult Update([FromForm] SolutionUpdateDto solutionUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("Kullanıcı girişi gereklidir.");

            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
                institutionId = Convert.ToInt32(institutionClaim.Value);

            int minSolutionLength = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MinSolutionLength", "50"));
            if (!string.IsNullOrEmpty(solutionUpdateDto.Description) && solutionUpdateDto.Description.Trim().Length < minSolutionLength)
                return BadRequest(new { success = false, message = $"Çözüm açıklaması en az {minSolutionLength} karakter olmalıdır." });

            int maxSolutionImageCount = int.Parse(_institutionFeatureService.GetFeatureValue(institutionId, "Content.MaxSolutionImageCount", "3"));
            var existingImagesList = string.IsNullOrEmpty(solutionUpdateDto.ImageUrls)
                ? new List<string>()
                : solutionUpdateDto.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (solutionUpdateDto.Images != null && (existingImagesList.Count + solutionUpdateDto.Images.Count) > maxSolutionImageCount)
                return BadRequest(new { success = false, message = $"Toplamda en fazla {maxSolutionImageCount} görsel yükleyebilirsiniz." });

            if (solutionUpdateDto.Images != null && solutionUpdateDto.Images.Count > 0 && !_institutionFeatureService.IsFeatureEnabled(institutionId, "Content.AllowSolutionImageUpload", true))
                return BadRequest(new { success = false, message = "Çözümler için görsel yükleme özelliği devre dışı." });

            bool videoEnabled = _institutionFeatureService.IsFeatureEnabled(institutionId, "Content.EnableVideo", false);
            if (solutionUpdateDto.Videos != null && solutionUpdateDto.Videos.Count > 0 && !videoEnabled)
                return BadRequest(new { success = false, message = "Bu kurum için video yükleme özelliği devre dışı." });

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "solutions");

            // Kaldırılan görselleri sil — GetById Solution entity döndürür (CSV string)
            var currentSolution = _solutionService.GetById(solutionUpdateDto.Id).Data;
            if (currentSolution?.ImageUrls != null)
            {
                var currentImages = currentSolution.ImageUrls
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                var removedImages = currentImages.Except(existingImagesList).ToList();
                foreach (var img in removedImages)
                    Core.Utilities.Helpers.FileHelper.FileHelper.Delete(img, uploadPath);
            }

            if (solutionUpdateDto.Images != null && solutionUpdateDto.Images.Count > 0)
            {
                foreach (var file in solutionUpdateDto.Images)
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

            // Yeni videolar
            if (result.Success && videoEnabled && solutionUpdateDto.Videos != null && solutionUpdateDto.Videos.Count > 0)
            {
                foreach (var vFile in solutionUpdateDto.Videos)
                {
                    try
                    {
                        var vName = Core.Utilities.Helpers.FileHelper.FileHelper.AddVideo(vFile, uploadPath);
                        if (!string.IsNullOrEmpty(vName))
                        {
                            _mediaAssetService.Add(new MediaAsset
                            {
                                OwnerType   = "Solution",
                                OwnerId     = solutionUpdateDto.Id,
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
        [Authorize]
        public IActionResult Delete(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("Kullanıcı girişi gereklidir.");

            var result = _solutionService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}/voters")]
        public IActionResult GetVoters(int id)
        {
            var solutionResult = _solutionService.GetById(id);
            if (!solutionResult.Success || solutionResult.Data == null) return NotFound();

            var visValue = _institutionFeatureService.GetFeatureValue(
                solutionResult.Data.InstitutionId, "Social.SolutionVotersVisibility", "admin_and_owner");
            var level = VisibilityHelper.Parse(visValue);
            if (!VisibilityHelper.CanView(level, GetCurrentUserId(), solutionResult.Data.SenderId, _capabilityResolver))
                return Forbid();

            var result = _solutionVoteService.GetVoters(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
