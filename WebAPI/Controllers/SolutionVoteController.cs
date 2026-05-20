using Business.Abstract;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolutionVoteController : Controller
    {
        private readonly ISolutionVoteService _solutionVoteService;
        private readonly IInstitutionFeatureService _institutionFeatureService;

        public SolutionVoteController(ISolutionVoteService solutionVoteService,
            IInstitutionFeatureService institutionFeatureService)
        {
            _solutionVoteService = solutionVoteService;
            _institutionFeatureService = institutionFeatureService;
        }

        [HttpPost("vote")]
        [Authorize]
        public IActionResult Vote([FromBody] SolutionVoteAddDto solutionVoteAddDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

            // Upvote feature kontrolü
            int institutionId = 1;
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null)
            {
                institutionId = Convert.ToInt32(institutionClaim.Value);
            }
            if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Social.EnableUpvote", true))
                return BadRequest(new { success = false, message = "Bu kurum için oylama özelliği devre dışı." });

            var result = _solutionVoteService.Vote(solutionVoteAddDto.SolutionId, solutionVoteAddDto.IsUpvote);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("getvotecount")]
        public IActionResult GetVoteCount(int solutionId)
        {
            var result = _solutionVoteService.GetSolutionVoteCount(solutionId);

            if (result.Success)
            {
                return Ok(result.Data);
            }
            return BadRequest(result.Message);
        }
    }
}