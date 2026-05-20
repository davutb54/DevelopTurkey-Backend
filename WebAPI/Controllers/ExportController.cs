using Business.Abstract;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExportController : Controller
    {
        private readonly IProblemService _problemService;

        public ExportController(IProblemService problemService)
        {
            _problemService = problemService;
        }

        [HttpGet("problems")]
        public IActionResult ExportProblems()
        {
            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
            {
                institutionId = int.Parse(institutionClaim.Value);
            }

            var filter = new ProblemFilterDto { Page = 1, PageSize = 10000 };
            var result = _problemService.GetList(filter, institutionId);
            if (!result.Success || result.Data == null)
                return BadRequest(new { success = false, message = "Veri alınamadı." });

            var sb = new StringBuilder();
            // CSV Başlık
            sb.AppendLine("Id,Title,Description,CityCode,SenderId,SendDate,IsResolved,ViewCount,SolutionCount,UpvoteCount");

            foreach (var problem in result.Data)
            {
                var title = (problem.Title ?? "").Replace("\"", "\"\"");
                var description = (problem.Description ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"{problem.Id},\"{title}\",\"{description}\",{problem.CityCode},{problem.SenderId},{problem.SendDate:yyyy-MM-dd HH:mm:ss},{problem.IsResolved},{problem.ViewCount},{problem.SolutionCount},{problem.UpvoteCount}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"problems_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}