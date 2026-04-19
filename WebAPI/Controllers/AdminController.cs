using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using Entities.DTOs;
using System;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IProblemService _problemService;
        private readonly ISolutionService _solutionService;
        private readonly ILogService _logService;
        private readonly ITopicService _topicService;
        private readonly IAdminService _adminService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISystemSettingsService _systemSettingsService;

        public AdminController(IUserService userService, IProblemService problemService,
            ISolutionService solutionService, ILogService logService, ITopicService topicService,
            IAdminService adminService, IWebHostEnvironment webHostEnvironment,
            ISystemSettingsService systemSettingsService)
        {
            _userService = userService;
            _problemService = problemService;
            _solutionService = solutionService;
            _logService = logService;
            _topicService = topicService;
            _adminService = adminService;
            _webHostEnvironment = webHostEnvironment;
            _systemSettingsService = systemSettingsService;
        }

        [HttpPost("banuser")]
        public IActionResult BanUser(int userId)
        {
            var result = _userService.BanUser(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }


        [HttpGet("getreportedproblems")]
        public IActionResult GetReportedProblems()
        {
            var result = _problemService.GetReportedProblems();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("deleteproblem")]
        public IActionResult DeleteProblem(int id)
        {
            var result = _problemService.Delete(id);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("dashboard")]
        public IActionResult GetDashboardStats()
        {
            var result = _adminService.GetDashboardStats();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("analytics")]
        public IActionResult GetDashboardAnalytics()
        {
            var result = _adminService.GetDashboardAnalytics();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("health")]
        public IActionResult GetSystemHealthStatus()
        {
            var result = _adminService.GetSystemHealthStatus();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("unbanuser")]
        public IActionResult UnbanUser(int userId)
        {
            var result = _userService.UnbanUser(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("impersonate")]
        [Authorize(Roles = "Admin")]
        public IActionResult ImpersonateUser([FromBody] ImpersonateDto impersonateDto)
        {
            var adminIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var actorClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor);

            if (adminIdClaim == null) return Unauthorized("Admin kimliği okunamadı.");
            int adminId = int.Parse(adminIdClaim.Value);

            // Güvenlik: Zaten başkasının rolündeyse zincirleme engelle (Recursive Impersonate Block)
            if (actorClaim != null)
            {
                _logService.LogWarning("Security", "Impersonate", $"Recursive (zincirleme) geçiş girişimi engellendi - AdminID: {adminId}");
                return BadRequest("Halihazırda başka bir kullanıcı olarak işlem yapıyorsunuz. Başka bir geçiş daha yapamazsınız.");
            }

            // Güvenlik: Admin şifre onayı (Sudo Mode)
            if (!_userService.VerifyPassword(adminId, impersonateDto.AdminPassword))
            {
                _logService.LogWarning("Security", "Impersonate", $"Hatalı sudo şifresi - AdminID: {adminId}, Hedef UID: {impersonateDto.TargetUserId}");
                return BadRequest("Hatalı güvenlik şifresi.");
            }

            var targetUserRes = _userService.GetById(impersonateDto.TargetUserId);
            if (!targetUserRes.Success || targetUserRes.Data == null) return BadRequest(targetUserRes.Message);

            // Güvenlik: Admin'den Admin'e geçiş yasak
            if (targetUserRes.Data.IsAdmin)
            {
                _logService.LogWarning("Security", "Impersonate", $"Admin'den Admin'e geçiş engellendi - AdminID: {adminId}, Hedef UID: {impersonateDto.TargetUserId}");
                return BadRequest("Güvenlik ihlali: Başka bir Sistem Yöneticisi hesabına geçiş yapamazsınız.");
            }

            var targetUserEntity = _userService.GetByUserName(targetUserRes.Data.UserName);
            if (targetUserEntity.IsBanned) return BadRequest("Hedef kullanıcı sistemden engellenmiş.");

            // Token'ı üret ve içerisine "Actor=adminId" claim'ini göm
            var tokenResult = _userService.CreateAccessToken(targetUserEntity, adminId);
            if (!tokenResult.Success) return BadRequest(tokenResult.Message);

            _logService.LogInfo("Security", "Impersonate", $"Admin (ID:{adminId}) -> User (ID:{targetUserEntity.Id}, {targetUserEntity.UserName}) hesabına sudo geçişi sağladı.");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.Extensions.Hosting.HostEnvironmentEnvExtensions.IsDevelopment(_webHostEnvironment) ? SameSiteMode.None : SameSiteMode.Strict,
                Expires = tokenResult.Data.Expiration
            };

            Response.Cookies.Append("token", tokenResult.Data.Token, cookieOptions);
            Response.Cookies.Append("userId", tokenResult.Data.UserId.ToString(), cookieOptions);

            return Ok(new { success = true, data = tokenResult.Data, message = "Kullanıcı hesabına geçiş başarılı." });
        }

        [HttpGet("getlogs")]
        public IActionResult GetLogs([FromQuery] Entities.DTOs.LogFilterDto filter)
        {
            var result = _logService.GetListByFilter(filter);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("toggleadminrole")]
        public IActionResult ToggleAdminRole(int userId)
        {
            var result = _userService.ToggleAdminRole(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("toggleexpertrole")]
        public IActionResult ToggleExpertRole(int userId)
        {
            var result = _userService.ToggleExpertRole(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("toggleofficialrole")]
        public IActionResult ToggleOfficialRole(int userId)
        {
            var result = _userService.ToggleOfficialRole(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("toggleproblemhighlight")]
        public IActionResult ToggleProblemHighlight(int problemId)
        {
            var result = _problemService.ToggleHighlight(problemId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("togglesolutionhighlight")]
        public IActionResult ToggleSolutionHighlight(int solutionId)
        {
            var result = _solutionService.ToggleHighlight(solutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("toggleproblemresolved")]
        public IActionResult ToggleProblemResolved(int problemId)
        {
            var result = _problemService.ToggleResolved(problemId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getpendingexpertsolutions")]
        public IActionResult GetPendingExpertSolutions()
        {
            var result = _solutionService.GetPendingExpertSolutions();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("approvesolution")]
        public IActionResult ApproveSolution(int solutionId)
        {
            var result = _solutionService.ApproveSolution(solutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("rejectsolution")]
        public IActionResult RejectSolution(int solutionId)
        {
            var result = _solutionService.RejectSolution(solutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getallproblems")]
        public IActionResult GetAllProblems()
        {
            var result = _problemService.GetAllForAdmin();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getallsolutions")]
        public IActionResult GetAllSolutions()
        {
            var result = _solutionService.GetAllForAdmin();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("removetopicfromproblem")]
        public IActionResult RemoveTopicFromProblem(int problemId, int topicId)
        {
            var result = _problemService.RemoveTopicFromProblem(problemId, topicId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getalltopics")]
        public IActionResult GetAllTopics()
        {
            var result = _topicService.GetAllForAdmin();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("systemsettings/get")]
        public IActionResult GetSystemSettings()
        {
            var result = _systemSettingsService.Get();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("systemsettings/update")]
        public IActionResult UpdateSystemSettings([FromBody] SystemSettings settings)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = _systemSettingsService.Update(settings, adminId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}