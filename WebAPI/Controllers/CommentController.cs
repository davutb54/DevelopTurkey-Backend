using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CommentController : Controller
	{
		private readonly ICommentService _commentService;

		public CommentController(ICommentService commentService)
		{
			_commentService = commentService;
		}

        [HttpGet("getbyid")]
		public IActionResult GetById(int id)
		{
			var result = _commentService.GetById(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getall")]
		public IActionResult GetAll()
		{
			var result = _commentService.GetAll();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbyparentcommentid")]
		public IActionResult GetByParentCommentId(int parentCommentId)
		{
			var result = _commentService.GetByParentCommentId(parentCommentId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbysolution")]
		public IActionResult GetBySolution(int solutionId)
		{
			var result = _commentService.GetBySolution(solutionId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("add")]
		[Authorize]
		public IActionResult Add(Comment comment)
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

			comment.SenderId = Convert.ToInt32(userIdClaim.Value);
			comment.SendDate = DateTime.Now;
			comment.IsDeleted = false;

			var result = _commentService.Add(comment);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
		[Authorize]
		public IActionResult Update(Comment comment)
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

			int currentUserId = Convert.ToInt32(userIdClaim.Value);

			var isAdminClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Admin");
			bool isAdmin = isAdminClaim != null;

			var existingComment = _commentService.GetById(comment.Id);
			if (!existingComment.Success || existingComment.Data == null)
			{
				return NotFound("Yorum bulunamadı.");
			}
			
			if (!isAdmin && existingComment.Data.SenderId != currentUserId)
			{
				return Forbid("Bu yorumu güncelleme yetkiniz yok.");
			}

			var result = _commentService.Update(comment);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpDelete("delete")]
		[Authorize]
		public IActionResult Delete(int id)
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

			int currentUserId = Convert.ToInt32(userIdClaim.Value);

			var isAdminClaim = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Admin");
			bool isAdmin = isAdminClaim != null;

			var existingComment = _commentService.GetById(id);
			if (!existingComment.Success || existingComment.Data == null)
			{
				return NotFound("Yorum bulunamadı.");
			}

			if (!isAdmin && existingComment.Data.SenderId != currentUserId)
			{
				return Forbid("Bu yorumu silme yetkiniz yok.");
			}

			var result = _commentService.Delete(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
