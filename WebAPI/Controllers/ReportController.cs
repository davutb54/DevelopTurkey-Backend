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
        public IActionResult Add([FromBody] ReportAddDto reportAddDto)
        {
            var report = new Report
            {
                ReporterUserId = reportAddDto.ReporterUserId,
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