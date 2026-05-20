using Business.Abstract;
using Core.Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.User;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
            var result = _userService.GetById(id);
            return Ok(result);
        }

        [HttpGet("getpublicprofile")]
        public IActionResult GetPublicProfile([FromQuery] int id, [FromQuery] int institutionId)
        {
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
            var result = _userService.GetPublicProfileByUserName(username, institutionId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getall")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult GetAll()
        {
            var result = _userService.GetAll();
            return Ok(result);
        }

        [HttpGet("getallpaged")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult GetAllPaged([FromQuery] UserFilterDto filter)
        {
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
                IsAdmin = u.IsAdmin,
                IsExpert = u.IsExpert,
                IsOfficial = u.IsOfficial,
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
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
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
