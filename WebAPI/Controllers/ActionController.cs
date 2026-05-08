using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ActionController : ControllerBase
    {
        private readonly IProblemFollowService _problemFollowService;
        private readonly ITopicFollowService _topicFollowService;
        private readonly ISavedSolutionService _savedSolutionService;

        public ActionController(
            IProblemFollowService problemFollowService,
            ITopicFollowService topicFollowService,
            ISavedSolutionService savedSolutionService)
        {
            _problemFollowService = problemFollowService;
            _topicFollowService = topicFollowService;
            _savedSolutionService = savedSolutionService;
        }

        private int GetUserId()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return 0;
            return int.Parse(userIdStr);
        }

        [HttpPost("toggle-problem-follow")]
        public IActionResult ToggleProblemFollow([FromQuery] int problemId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = _problemFollowService.ToggleFollow(problemId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("toggle-topic-follow")]
        public IActionResult ToggleTopicFollow([FromQuery] int topicId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = _topicFollowService.ToggleFollow(topicId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("toggle-solution-save")]
        public IActionResult ToggleSolutionSave([FromQuery] int solutionId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = _savedSolutionService.ToggleSave(solutionId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("check-problem-follow")]
        public IActionResult CheckProblemFollow([FromQuery] int problemId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var isFollowing = _problemFollowService.CheckFollow(problemId, userId);
            return Ok(new { isFollowing });
        }

        [HttpGet("check-solution-save")]
        public IActionResult CheckSolutionSave([FromQuery] int solutionId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var isSaved = _savedSolutionService.CheckSave(solutionId, userId);
            return Ok(new { isSaved });
        }
    }
}
