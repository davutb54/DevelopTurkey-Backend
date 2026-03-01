using Business.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("add")]
        [Authorize]
        public IActionResult Add([FromBody] ReportAddDto reportAddDto)
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

            
            int authenticatedUserId = Convert.ToInt32(userIdClaim.Value);

            var report = new Report
            {
                ReporterUserId = authenticatedUserId,
                TargetType = reportAddDto.TargetType,
                TargetId = reportAddDto.TargetId,
                Reason = reportAddDto.Reason
            };

            var result = _reportService.Add(report);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getall")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAll()
        {
            var result = _reportService.GetAll();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getpending")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetPending()
        {
            var result = _reportService.GetPendingReports();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("resolve")]
        [Authorize(Roles = "Admin")]
        public IActionResult Resolve(int id)
        {
            var result = _reportService.ResolveReport(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var result = _reportService.Delete(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}