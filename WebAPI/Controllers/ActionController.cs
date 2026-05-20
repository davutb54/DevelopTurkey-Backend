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
        private readonly IProblemUpvoteService _problemUpvoteService;
        private readonly IInstitutionFeatureService _institutionFeatureService;

        public ActionController(
            IProblemFollowService problemFollowService,
            ITopicFollowService topicFollowService,
            ISavedSolutionService savedSolutionService,
            IProblemUpvoteService problemUpvoteService,
            IInstitutionFeatureService institutionFeatureService)
        {
            _problemFollowService = problemFollowService;
            _topicFollowService = topicFollowService;
            _savedSolutionService = savedSolutionService;
            _problemUpvoteService = problemUpvoteService;
            _institutionFeatureService = institutionFeatureService;
        }

        private int GetUserId()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return 0;
            return int.Parse(userIdStr);
        }

        private int GetInstitutionId()
        {
            var institutionClaim = User.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
            if (institutionClaim != null && int.TryParse(institutionClaim.Value, out int instId))
                return instId;
            return 1;
        }

        [HttpPost("toggle-problem-follow")]
        public IActionResult ToggleProblemFollow([FromQuery] int problemId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var institutionId = GetInstitutionId();
            if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Social.EnableFollowSystem", true))
                return BadRequest(new { success = false, message = "Takip sistemi kurumunuz için devre dışı." });

            var result = _problemFollowService.ToggleFollow(problemId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("toggle-topic-follow")]
        public IActionResult ToggleTopicFollow([FromQuery] int topicId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var institutionId = GetInstitutionId();
            if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Social.EnableFollowSystem", true))
                return BadRequest(new { success = false, message = "Takip sistemi kurumunuz için devre dışı." });

            var result = _topicFollowService.ToggleFollow(topicId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("toggle-solution-save")]
        public IActionResult ToggleSolutionSave([FromQuery] int solutionId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var institutionId = GetInstitutionId();
            if (!_institutionFeatureService.IsFeatureEnabled(institutionId, "Social.EnableSavedSolutions", true))
                return BadRequest(new { success = false, message = "Çözüm kaydetme özelliği kurumunuz için devre dışı." });

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

        [HttpGet("check-topic-follow")]
        public IActionResult CheckTopicFollow([FromQuery] int topicId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var isFollowing = _topicFollowService.CheckFollow(topicId, userId);
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

        [HttpPost("toggle-problem-upvote")]
        public IActionResult ToggleProblemUpvote([FromQuery] int problemId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = _problemUpvoteService.ToggleUpvote(problemId, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("check-problem-upvote")]
        public IActionResult CheckProblemUpvote([FromQuery] int problemId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var isUpvoted = _problemUpvoteService.CheckUpvote(problemId, userId);
            return Ok(new { isUpvoted });
        }

        [HttpGet("get-problem-upvote-count")]
        [AllowAnonymous]
        public IActionResult GetProblemUpvoteCount([FromQuery] int problemId)
        {
            var count = _problemUpvoteService.GetUpvoteCount(problemId);
            return Ok(new { count });
        }

        [HttpGet("get-my-saved-solutions")]
        public IActionResult GetMySavedSolutions()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = _savedSolutionService.GetSavedSolutions(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
