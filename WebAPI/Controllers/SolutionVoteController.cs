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

        public SolutionVoteController(ISolutionVoteService solutionVoteService)
        {
            _solutionVoteService = solutionVoteService;
        }

        [HttpPost("vote")]
        [Authorize]
        public IActionResult Vote([FromBody] SolutionVoteAddDto solutionVoteAddDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("Kullanıcı girişi gereklidir.");
            }

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