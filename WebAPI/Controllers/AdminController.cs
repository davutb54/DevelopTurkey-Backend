using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Authorization;
using Core.Utilities.Hashing;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using Entities.DTOs;
using System;
using WebAPI.Filters;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        private readonly IUserWarningService _userWarningService;
        private readonly INotificationService _notificationService;
        private readonly ILegalAgreementService _legalAgreementService;
        private readonly IAboutPageSectionService _aboutPageSectionService;
        private readonly IInstitutionFeatureService _institutionFeatureService;
        private readonly IWorkflowEventBus _eventBus;
        private readonly ICapabilityResolver _capabilityResolver;
        private readonly IHashidsService _hashids;

        public AdminController(IUserService userService, IProblemService problemService,
            ISolutionService solutionService, ILogService logService, ITopicService topicService,
            IAdminService adminService, IWebHostEnvironment webHostEnvironment,
            ISystemSettingsService systemSettingsService,
            IUserWarningService userWarningService, INotificationService notificationService,
            ILegalAgreementService legalAgreementService,
            IAboutPageSectionService aboutPageSectionService,
            IInstitutionFeatureService institutionFeatureService,
            IWorkflowEventBus eventBus,
            ICapabilityResolver capabilityResolver,
            IHashidsService hashids)
        {
            _userService = userService;
            _problemService = problemService;
            _solutionService = solutionService;
            _logService = logService;
            _topicService = topicService;
            _adminService = adminService;
            _webHostEnvironment = webHostEnvironment;
            _systemSettingsService = systemSettingsService;
            _userWarningService = userWarningService;
            _notificationService = notificationService;
            _legalAgreementService = legalAgreementService;
            _aboutPageSectionService = aboutPageSectionService;
            _institutionFeatureService = institutionFeatureService;
            _eventBus = eventBus;
            _capabilityResolver = capabilityResolver;
            _hashids = hashids;
        }

        private void EncodeProblems(IEnumerable<ProblemDetailDto>? items)
        {
            if (items == null) return;
            foreach (var p in items) p.PublicId = _hashids.Encode(p.Id);
        }

        private void EncodeSolutions(IEnumerable<SolutionDetailDto>? items)
        {
            if (items == null) return;
            foreach (var s in items)
            {
                s.PublicId        = _hashids.Encode(s.Id);
                s.ProblemPublicId = _hashids.Encode(s.ProblemId);
            }
        }

        [HttpPost("banuser")]
        [RequireCapability("admin.user_ban")]
        public IActionResult BanUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!HasInstitutionAccess(currentUserId, "admin.user_ban", userId))
                return Forbid();

            var result = _userService.BanUser(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }


        [HttpGet("getreportedproblems")]
        [RequireCapability("moderation.content_review")]
        public IActionResult GetReportedProblems()
        {
            var actorId = GetActorId();
            var inst = GetScopedInstitution(actorId, "moderation.content_review");
            if (inst == -1) return Forbid();
            var result = _problemService.GetReportedProblems(inst);
            EncodeProblems(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("deleteproblem")]
        [RequireCapability("moderation.problem_delete")]
        public IActionResult DeleteProblem(int id)
        {
            var actorId = GetActorId();
            if (!HasProblemAccess(actorId, "moderation.problem_delete", id)) return Forbid();
            var result = _problemService.Delete(id);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("dashboard")]
        [RequireCapability("admin.dashboard_view")]
        public IActionResult GetDashboardStats()
        {
            var result = _adminService.GetDashboardStats();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("analytics")]
        [RequireCapability("admin.metrics_user_view")]
        public IActionResult GetDashboardAnalytics()
        {
            var result = _adminService.GetDashboardAnalytics();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("health")]
        [RequireCapability("admin.metrics_system_health_view")]
        public IActionResult GetSystemHealthStatus()
        {
            var result = _adminService.GetSystemHealthStatus();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("unbanuser")]
        [RequireCapability("admin.user_unban")]
        public IActionResult UnbanUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!HasInstitutionAccess(currentUserId, "admin.user_unban", userId))
                return Forbid();

            var result = _userService.UnbanUser(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("impersonate")]
        [RequireCapability("admin.user_impersonate")]
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

            // KRİTİK: Impersonation feature kontrolü hedef kullanıcının kurumuna göre yapılır
            int targetInstitutionId = targetUserRes.Data.InstitutionId;
            if (!_institutionFeatureService.IsFeatureEnabled(targetInstitutionId, "Identity.AllowImpersonation", false))
                return BadRequest(new { success = false, message = "Bu kullanıcının kurumu için yönetici geçişi özelliği devre dışı bırakılmış." });

            // Güvenlik: Admin capability'sine sahip kullanıcıya geçiş yasak
            if (_capabilityResolver.Allows(targetUserRes.Data.Id, "admin.system_access"))
            {
                _logService.LogWarning("Security", "Impersonate", $"Admin'den Admin'e geçiş engellendi - AdminID: {adminId}, Hedef UID: {impersonateDto.TargetUserId}");
                return BadRequest("Güvenlik ihlali: Başka bir Sistem Yöneticisi hesabına geçiş yapamazsınız.");
            }

            var targetUserEntity = _userService.GetByUserName(targetUserRes.Data.UserName);
            if (targetUserEntity.IsBanned) return BadRequest("Hedef kullanıcı sistemden engellenmiş.");

            // Token'ı üret ve içerisine "Actor=adminId" claim'ini göm
            var tokenResult = _userService.CreateAccessToken(targetUserEntity, adminId);
            if (!tokenResult.Success) return BadRequest(tokenResult.Message);

            _ = _eventBus.PublishAsync("auth.impersonated", new RuleContext
            {
                SystemUserId = adminId,
                TargetUserId = targetUserEntity.Id,
                Metadata = new Dictionary<string, object?>
                {
                    ["AdminId"] = adminId,
                    ["TargetUserId"] = targetUserEntity.Id
                }
            });

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
        [RequireCapability("admin.audit_read")]
        public IActionResult GetLogs([FromQuery] Entities.DTOs.LogFilterDto filter)
        {
            var result = _logService.GetListByFilter(filter);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("changeuserinstitution")]
        [RequireCapability("admin.user_update")]
        public IActionResult ChangeUserInstitution(int userId, int newInstitutionId)
        {
            var actorId = GetActorId();
            if (!HasUserAccess(actorId, "admin.user_update", userId)) return Forbid();
            var result = _userService.ChangeUserInstitution(userId, newInstitutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("toggleproblemhighlight")]
        [RequireCapability("moderation.problem_highlight")]
        public IActionResult ToggleProblemHighlight(int problemId)
        {
            var actorId = GetActorId();
            if (!HasProblemAccess(actorId, "moderation.problem_highlight", problemId)) return Forbid();
            var result = _problemService.ToggleHighlight(problemId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("togglesolutionhighlight")]
        [RequireCapability("moderation.solution_highlight")]
        public IActionResult ToggleSolutionHighlight(int solutionId)
        {
            var actorId = GetActorId();
            if (!HasSolutionAccess(actorId, "moderation.solution_highlight", solutionId)) return Forbid();
            var result = _solutionService.ToggleHighlight(solutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("toggleproblemresolved")]
        [RequireCapability("moderation.problem_resolve")]
        public IActionResult ToggleProblemResolved(int problemId)
        {
            var actorId = GetActorId();
            if (!HasProblemAccess(actorId, "moderation.problem_resolve", problemId)) return Forbid();
            var result = _problemService.ToggleResolved(problemId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getpendingexpertsolutions")]
        [RequireCapability("expert.solution_approve")]
        public IActionResult GetPendingExpertSolutions()
        {
            var actorId = GetActorId();
            var inst = GetScopedInstitution(actorId, "expert.solution_approve");
            if (inst == -1) return Forbid();
            var result = _solutionService.GetPendingExpertSolutions(inst);
            EncodeSolutions(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("approvesolution")]
        [RequireCapability("expert.solution_approve")]
        public IActionResult ApproveSolution(int solutionId)
        {
            var actorId = GetActorId();
            if (!HasSolutionAccess(actorId, "expert.solution_approve", solutionId)) return Forbid();
            var result = _solutionService.ApproveSolution(solutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("rejectsolution")]
        [RequireCapability("expert.solution_reject")]
        public IActionResult RejectSolution(int solutionId)
        {
            var actorId = GetActorId();
            if (!HasSolutionAccess(actorId, "expert.solution_reject", solutionId)) return Forbid();
            var result = _solutionService.RejectSolution(solutionId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getallproblems")]
        [RequireCapability("admin.user_read")]
        public IActionResult GetAllProblems()
        {
            var actorId = GetActorId();
            var inst = GetScopedInstitution(actorId, "admin.user_read");
            if (inst == -1) return Forbid();
            var result = _problemService.GetAllForAdmin(inst);
            EncodeProblems(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getallsolutions")]
        [RequireCapability("admin.user_read")]
        public IActionResult GetAllSolutions()
        {
            var actorId = GetActorId();
            var inst = GetScopedInstitution(actorId, "admin.user_read");
            if (inst == -1) return Forbid();
            var result = _solutionService.GetAllForAdmin(inst);
            EncodeSolutions(result.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("removetopicfromproblem")]
        [RequireCapability("moderation.problem_moderate")]
        public IActionResult RemoveTopicFromProblem(int problemId, int topicId)
        {
            var actorId = GetActorId();
            if (!HasProblemAccess(actorId, "moderation.problem_moderate", problemId)) return Forbid();
            var result = _problemService.RemoveTopicFromProblem(problemId, topicId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getalltopics")]
        [RequireCapability("admin.user_read")]
        public IActionResult GetAllTopics()
        {
            var result = _topicService.GetAllForAdmin();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("systemsettings/get")]
        [RequireCapability("admin.system_settings_read")]
        public IActionResult GetSystemSettings()
        {
            var result = _systemSettingsService.Get();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("systemsettings/update")]
        [RequireCapability("admin.system_settings_write")]
        public IActionResult UpdateSystemSettings([FromBody] SystemSettings settings)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = _systemSettingsService.Update(settings, adminId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("systemsettings/public")]
        [AllowAnonymous]
        public IActionResult GetPublicSettings()
        {
            var result = _systemSettingsService.Get();
            if (!result.Success) return BadRequest();
            return Ok(new
            {
                siteName         = result.Data.SiteName,
                siteDescription  = result.Data.SiteDescription,
                organizationName = result.Data.OrganizationName,
                contactFullName  = result.Data.ContactFullName,
                contactAddress   = result.Data.ContactAddress,
                contactEmail     = result.Data.ContactEmail,
                contactPhone     = result.Data.ContactPhone,
                socialTwitter    = result.Data.SocialTwitter,
                socialInstagram  = result.Data.SocialInstagram,
                socialLinkedIn   = result.Data.SocialLinkedIn,
            });
        }

        // --- KULLANICI UYARI YÖNETİMİ ---

        [HttpPost("issue-warning")]
        [RequireCapability("moderation.user_warn")]
        public IActionResult IssueWarning([FromBody] IssueWarningDto dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!HasInstitutionAccess(adminId, "moderation.user_warn", dto.UserId))
                return Forbid();
            var warning = new UserWarning
            {
                UserId = dto.UserId,
                IssuedByAdminId = adminId,
                Title = dto.Title,
                Message = dto.Message,
                Severity = dto.Severity
            };
            var result = _userWarningService.Issue(warning);
            if (!result.Success) return BadRequest(result);

            try
            {
                _notificationService.Add(new Notification
                {
                    UserId = dto.UserId,
                    Title = $"⚠️ {dto.Title}",
                    Message = dto.Message,
                    Type = "AdminWarning",
                    ReferenceLink = null
                });
            }
            catch { /* Bildirim hatası uyarı işlemini etkilemesin */ }

            return Ok(result);
        }

        [HttpPost("revoke-warning")]
        [RequireCapability("moderation.user_warn_revoke")]
        public IActionResult RevokeWarning(int warningId)
        {
            var result = _userWarningService.Revoke(warningId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user-warnings")]
        [RequireCapability("admin.user_warning_read_all")]
        public IActionResult GetUserWarnings(int userId)
        {
            var actorId = GetActorId();
            if (!HasUserAccess(actorId, "admin.user_warning_read_all", userId)) return Forbid();
            var result = _userWarningService.GetByUserId(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ─── SÖZLEŞME YÖNETİMİ ───────────────────────────────────────────────────

        [HttpGet("agreements")]
        [RequireCapability("admin.legal_agreement_manage")]
        public IActionResult GetAllAgreements()
        {
            var result = _legalAgreementService.GetAll();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("agreements/{id}")]
        [RequireCapability("admin.legal_agreement_manage")]
        public IActionResult GetAgreementById(int id)
        {
            var result = _legalAgreementService.GetById(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("agreements")]
        [RequireCapability("admin.legal_agreement_manage")]
        public IActionResult CreateAgreement([FromBody] Entities.DTOs.CreateAgreementDto dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = _legalAgreementService.Create(dto, adminId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("agreements/activate/{id}")]
        [RequireCapability("admin.legal_agreement_manage")]
        public IActionResult ActivateAgreement(int id)
        {
            var result = _legalAgreementService.Activate(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("agreements/{id}")]
        [RequireCapability("admin.legal_agreement_manage")]
        public IActionResult DeleteAgreement(int id)
        {
            var result = _legalAgreementService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("agreements/{id}/stats")]
        [RequireCapability("admin.legal_agreement_manage")]
        public IActionResult GetAgreementStats(int id)
        {
            var countResult = _legalAgreementService.GetAcceptanceCount(id);
            var rateResult = _legalAgreementService.GetAcceptanceRate(id);

            if (!countResult.Success || !rateResult.Success)
                return BadRequest("İstatistikler alınırken bir hata oluştu.");

            return Ok(new Entities.DTOs.AgreementStatsDto
            {
                AgreementId = id,
                AcceptanceCount = countResult.Data,
                AcceptanceRate = rateResult.Data
            });
        }

        // --- About Page Sections ---

        [HttpGet("aboutsections")]
        [RequireCapability("admin.about_page_manage")]
        public IActionResult GetAllAboutSections()
        {
            var result = _aboutPageSectionService.GetAll();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("aboutsections")]
        [RequireCapability("admin.about_page_manage")]
        public IActionResult AddAboutSection([FromBody] AboutPageSection section)
        {
            var result = _aboutPageSectionService.Add(section);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("aboutsections")]
        [RequireCapability("admin.about_page_manage")]
        public IActionResult UpdateAboutSection([FromBody] AboutPageSection section)
        {
            var result = _aboutPageSectionService.Update(section);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("aboutsections/{id}")]
        [RequireCapability("admin.about_page_manage")]
        public IActionResult DeleteAboutSection(int id)
        {
            var result = _aboutPageSectionService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── YARDIMCI METODLAR ─────────────────────────────────────────────────────

        private int GetActorId()
            => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Çağıranın bu yetki için kapsam durumunu döner:
        //   null  → global admin, hiçbir kısıtlama yok
        //   -2    → global admin (null yerine integer context gereken yerlerde kullan)
        //   ≥ 0   → scoped, bu institution ID'sine kısıtlı
        //   -1    → erişim yok
        private int? GetScopedInstitution(int actorId, string capabilityCode)
        {
            if (_capabilityResolver.Allows(actorId, capabilityCode, ctx: null))
                return null; // globally granted

            var snapshot = HttpContext.RequestServices.GetService<ICapabilitySnapshot>();

            // admin.cross_institution_read sahipleri, sahip oldukları herhangi bir
            // capability'yi (scoped dahil) tüm kurumlar için global olarak kullanabilir.
            if (_capabilityResolver.Allows(actorId, "admin.cross_institution_read", ctx: null))
            {
                var now = DateTime.UtcNow;
                bool hasAnywhere = snapshot?.Get(actorId)
                    .Any(e => e.CapabilityCode.Equals(capabilityCode, StringComparison.OrdinalIgnoreCase)
                              && (!e.ExpiresAt.HasValue || e.ExpiresAt.Value > now)) ?? false;
                return hasAnywhere ? null : -1;
            }

            var clientContext = HttpContext.RequestServices.GetService<Core.Utilities.Context.IClientContext>();
            var actorInst = clientContext?.GetInstitutionId();

            if (snapshot == null || !actorInst.HasValue)
                return -1;

            var hasScoped = snapshot.Get(actorId)
                .Any(e => e.CapabilityCode.Equals(capabilityCode, StringComparison.OrdinalIgnoreCase)
                          && e.InstitutionId == actorInst.Value);

            return hasScoped ? actorInst.Value : -1;
        }

        private bool HasUserAccess(int actorId, string capabilityCode, int targetUserId)
        {
            var inst = GetScopedInstitution(actorId, capabilityCode);
            if (inst == null) return true;
            if (inst == -1) return false;
            var target = _userService.GetById(targetUserId);
            return target.Success && target.Data?.InstitutionId == inst.Value;
        }

        private bool HasProblemAccess(int actorId, string capabilityCode, int problemId)
        {
            var inst = GetScopedInstitution(actorId, capabilityCode);
            if (inst == null) return true;
            if (inst == -1) return false;
            return _problemService.GetProblemInstitution(problemId) == inst.Value;
        }

        private bool HasSolutionAccess(int actorId, string capabilityCode, int solutionId)
        {
            var inst = GetScopedInstitution(actorId, capabilityCode);
            if (inst == null) return true;
            if (inst == -1) return false;
            return _solutionService.GetSolutionInstitution(solutionId) == inst.Value;
        }

        // Eskiden HasInstitutionAccess olarak kullanılan metodun yeni adı
        [System.Obsolete("HasUserAccess kullanın.")]
        private bool HasInstitutionAccess(int actorId, string capabilityCode, int targetUserId)
            => HasUserAccess(actorId, capabilityCode, targetUserId);
    }
}