using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public AdminController(IUserService userService, IProblemService problemService,
            ISolutionService solutionService, ILogService logService)
        {
            _userService = userService;
            _problemService = problemService;
            _solutionService = solutionService;
            _logService = logService;
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
            var stats = new Entities.DTOs.AdminDashboardDto
            {
                TotalUsers = _userService.GetUserCount(),
                BannedUsers = _userService.GetBannedUserCount(),
                TotalProblems = _problemService.GetTotalCount(),
                ReportedProblems = _problemService.GetReportedCount(),
                TotalSolutions = _solutionService.GetTotalCount()
            };

            return Ok(new Core.Utilities.Results.SuccessDataResult<Entities.DTOs.AdminDashboardDto>(stats,
                "İstatistikler getirildi."));
        }

        [HttpPost("unbanuser")]
        public IActionResult UnbanUser(int userId)
        {
            var result = _userService.UnbanUser(userId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("getlogs")]
        public IActionResult GetLogs([FromQuery] Entities.DTOs.LogFilterDto filter)
        {
            var result = _logService.GetList(filter);
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
    }
}