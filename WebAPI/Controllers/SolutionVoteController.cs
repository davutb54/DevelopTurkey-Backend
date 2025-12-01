using Business.Abstract;
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
        public IActionResult Vote(int userId, int solutionId, bool isUpvote)
        {
            var result = _solutionVoteService.Vote(userId, solutionId, isUpvote);

            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
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