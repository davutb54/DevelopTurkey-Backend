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

        public AdminController(IUserService userService, IProblemService problemService, ISolutionService solutionService, ILogService logService)
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

            return Ok(new Core.Utilities.Results.SuccessDataResult<Entities.DTOs.AdminDashboardDto>(stats, "İstatistikler getirildi."));
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
    }
}