using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Entities.DTOs;
using Entities.DTOs.User;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using WebAPI.Filters;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogService _logService;

        public UserController(
            IUserService userService,
            IWebHostEnvironment webHostEnvironment,
            ILogService logService)
        {
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
            _logService = logService;
        }

        [HttpGet("getbyid")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetById(int id)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId) || currentUserId <= 0)
            {
                return Unauthorized("Geçersiz token.");
            }

            // Self-read her zaman serbest
            if (id == currentUserId)
            {
                var self = _userService.GetById(id);
                return Ok(self);
            }

            // Başkasının profil detayları sadece admin.user_read ile
            var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
            var clientContext = HttpContext.RequestServices.GetService<IClientContext>();
            var institutionId = clientContext?.GetInstitutionId();

            if (resolver == null)
            {
                return StatusCode(500, "Capability resolver bulunamadı.");
            }

            // Global admin (ctx=null) ise izin ver
            if (resolver.Allows(currentUserId, "admin.user_read", ctx: null))
            {
                var anyUser = _userService.GetById(id);
                return Ok(anyUser);
            }

            // Kurum-scoped admin ise sadece kendi kurumundaki kullanıcıları okuyabilsin
            if (!institutionId.HasValue)
            {
                return Forbid();
            }

            var ctx = new CapabilityRequestContext(InstitutionId: institutionId.Value);
            if (!resolver.Allows(currentUserId, "admin.user_read", ctx))
            {
                return Forbid();
            }

            var target = _userService.GetById(id);
            if (!target.Success || target.Data == null)
            {
                return Ok(target);
            }

            if (target.Data.InstitutionId != institutionId.Value)
            {
                return Forbid();
            }

            return Ok(target);
        }

        [HttpGet("getpublicprofile")]
        public IActionResult GetPublicProfile([FromQuery] int id, [FromQuery] int institutionId)
        {
            var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
            var clientContext = HttpContext.RequestServices.GetService<IClientContext>();
            var currentUserId = clientContext?.GetUserId() ?? 0;
            var currentInstitutionId = clientContext?.GetInstitutionId();

            var isGlobalAdminUserRead = resolver != null && currentUserId > 0 &&
                                        resolver.Allows(currentUserId, "admin.user_read", ctx: null);

            if (!isGlobalAdminUserRead && currentInstitutionId.HasValue)
            {
                institutionId = currentInstitutionId.Value;
            }

            var result = _userService.GetPublicProfile(id, institutionId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getpublicprofilebyusername")]
        public IActionResult GetPublicProfileByUserName([FromQuery] string username, [FromQuery] int institutionId)
        {
            var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
            var clientContext = HttpContext.RequestServices.GetService<IClientContext>();
            var currentUserId = clientContext?.GetUserId() ?? 0;
            var currentInstitutionId = clientContext?.GetInstitutionId();

            var isGlobalAdminUserRead = resolver != null && currentUserId > 0 &&
                                        resolver.Allows(currentUserId, "admin.user_read", ctx: null);

            if (!isGlobalAdminUserRead && currentInstitutionId.HasValue)
            {
                institutionId = currentInstitutionId.Value;
            }

            var result = _userService.GetPublicProfileByUserName(username, institutionId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getall")]
        [RequireCapability("admin.user_read")]
        public IActionResult GetAll()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                return Unauthorized();

            var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
            var snapshot = HttpContext.RequestServices.GetService<ICapabilitySnapshot>();

            if (resolver != null && !resolver.Allows(currentUserId, "admin.user_read", ctx: null))
            {
                var allowedIds = snapshot?.Get(currentUserId)
                    .Where(e => e.CapabilityCode.Equals("admin.user_read", StringComparison.OrdinalIgnoreCase)
                                && e.InstitutionId.HasValue)
                    .Select(e => e.InstitutionId!.Value)
                    .ToList();

                if (allowedIds == null || allowedIds.Count == 0)
                    return Forbid();

                var filtered = _userService.GetAllByInstitutions(allowedIds);
                return Ok(filtered);
            }

            var result = _userService.GetAll();
            return Ok(result);
        }

        [HttpGet("getallpaged")]
        [RequireCapability("admin.user_read")]
        public IActionResult GetAllPaged([FromQuery] UserFilterDto filter)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                return Unauthorized();

            var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
            var snapshot = HttpContext.RequestServices.GetService<ICapabilitySnapshot>();

            // Global scope: ya admin.user_read globally grant edilmiş ya da
            // admin.cross_institution_read ile kurum kısıtlaması kaldırılmış.
            bool isGlobalUserRead = resolver != null &&
                (resolver.Allows(currentUserId, "admin.user_read", ctx: null) ||
                 resolver.Allows(currentUserId, "admin.cross_institution_read", ctx: null));

            if (!isGlobalUserRead)
            {
                var allowedIds = snapshot?.Get(currentUserId)
                    .Where(e => e.CapabilityCode.Equals("admin.user_read", StringComparison.OrdinalIgnoreCase)
                                && e.InstitutionId.HasValue)
                    .Select(e => e.InstitutionId!.Value)
                    .ToList();

                if (allowedIds == null || allowedIds.Count == 0)
                    return Forbid();

                filter.AllowedInstitutionIds = allowedIds;
            }

            var result = _userService.GetAllPaged(filter);
            if (!result.Success) return BadRequest(result);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data.Items,
                totalCount = result.Data.TotalCount,
                page = filter.Page,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling((double)result.Data.TotalCount / filter.PageSize)
            });
        }

        [HttpGet("searchmentions")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult SearchUsersForMention([FromQuery] string searchText, [FromQuery] int? institutionId)
        {
            var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
            var clientContext = HttpContext.RequestServices.GetService<IClientContext>();
            var currentUserId = clientContext?.GetUserId() ?? 0;
            var currentInstitutionId = clientContext?.GetInstitutionId();

            var isGlobalAdminUserRead = resolver != null && currentUserId > 0 &&
                                        resolver.Allows(currentUserId, "admin.user_read", ctx: null);

            // Global admin değilse kurum parametresini token'dan zorla (cross-tenant enumeration engeli)
            if (!isGlobalAdminUserRead)
            {
                institutionId = currentInstitutionId;
            }

            var filter = new UserFilterDto
            {
                SearchText = searchText,
                InstitutionId = institutionId ?? 0,
                Page = 1,
                PageSize = 10
            };

            var result = _userService.GetAllPaged(filter);
            if (!result.Success) return BadRequest(result);

            var publicUsers = result.Data.Items.Select(u => new UserPublicProfileDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Name = u.Name,
                Surname = u.Surname,
                CityName = u.CityName,
                Gender = u.Gender,
                RegisterDate = u.RegisterDate,
                ProfileImageUrl = u.ProfileImageUrl,
                InstitutionId = u.InstitutionId
            }).ToList();

            return Ok(new { success = true, data = new { items = publicUsers } });
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetMe()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
            {
                var result = _userService.GetById(id);
                return Ok(result);
            }

            return Unauthorized("Geçersiz token.");
        }

        [HttpPost("updatedetails")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult UpdateDetails(UserForUpdateDto userForUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            if (User.Claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Actor))
            {
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının profil detaylarını güncelleyemezsiniz.");
            }

            var result = _userService.UpdateUserDetails(userForUpdateDto);
            return Ok(result);
        }

        [HttpPost("delete")]
        [RequireCapability("admin.user_delete")]
        public IActionResult Delete(int id)
        {
            var currentUserIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim.Value, out int actorId))
            {
                var resolver = HttpContext.RequestServices.GetService<ICapabilityResolver>();
                var snapshot = HttpContext.RequestServices.GetService<ICapabilitySnapshot>();
                var clientContext = HttpContext.RequestServices.GetService<IClientContext>();

                if (resolver != null && !resolver.Allows(actorId, "admin.user_delete", ctx: null))
                {
                    var actorInst = clientContext?.GetInstitutionId();
                    if (actorInst == null) return Forbid();

                    var hasScoped = snapshot?.Get(actorId)
                        .Any(e => e.CapabilityCode.Equals("admin.user_delete", StringComparison.OrdinalIgnoreCase)
                                  && e.InstitutionId == actorInst.Value) ?? false;
                    if (!hasScoped) return Forbid();

                    var target = _userService.GetById(id);
                    if (!target.Success || target.Data?.InstitutionId != actorInst.Value)
                        return Forbid();
                }
            }
            var result = _userService.DeleteUser(id);
            return Ok(result);
        }

        [HttpPost("checkuserexists")]
        [EnableRateLimiting("AuthLimit")]
        public IActionResult CheckUserExists(CheckExistsDto checkExistsDto)
        {
            var result = _userService.CheckUserExists(checkExistsDto);
            return Ok(result);
        }

        [HttpPost("uploadprofileimage")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult UploadProfileImage([FromForm] UserImageUpdateDto userImageUpdateDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            if (User.Claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Actor))
            {
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının profil resmini güncelleyemezsiniz.");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null) return Unauthorized("Geçersiz token.");
            int authenticatedUserId = Convert.ToInt32(userIdClaim.Value);

            var userDetail = _userService.GetById(authenticatedUserId);
            if (!userDetail.Success) return BadRequest(userDetail.Message);

            if (userImageUpdateDto.Image == null || userImageUpdateDto.Image.Length == 0)
            {
                return BadRequest("Lütfen bir resim dosyası seçin.");
            }

            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");

            try
            {
                string newFileName = Core.Utilities.Helpers.FileHelper.FileHelper.Add(userImageUpdateDto.Image, uploadPath);

                if (newFileName == null) return BadRequest("Dosya yüklenemedi.");

                var user = _userService.GetByUserName(userDetail.Data.UserName);

                // Eski profil resmini sil
                if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl))
                    Core.Utilities.Helpers.FileHelper.FileHelper.Delete(user.ProfileImageUrl, uploadPath);

                user.ProfileImageUrl = newFileName;
                _userService.Update(user);

                return Ok(new { message = "Profil resmi güncellendi", imageUrl = newFileName });
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

        [HttpPost("change-institution")]
        [RequireCapability("admin.user_institution_change")]
        public IActionResult ChangeInstitution([FromQuery] int userId, [FromQuery] int newInstitutionId)
        {
            var result = _userService.ChangeUserInstitution(userId, newInstitutionId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("updateusername")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult UpdateUsername([FromBody] string newUsername)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("Kullanıcı girişi gereklidir.");

            if (User.Claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Actor))
                return BadRequest("SUDO Güvenlik Politikası: Başka bir kullanıcının adını değiştiremezsiniz.");

            if (string.IsNullOrWhiteSpace(newUsername))
                return BadRequest("Kullanıcı adı boş olamaz.");

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Geçersiz token.");

            var result = _userService.UpdateUsername(userId, newUsername);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
