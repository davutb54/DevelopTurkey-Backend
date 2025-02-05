using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CommentController : Controller
	{
		private readonly ICommentService _commentService = new CommentManager();

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
		public IActionResult Add(Comment comment)
		{
			var result = _commentService.Add(comment);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
		public IActionResult Update(Comment comment)
		{
			var result = _commentService.Update(comment);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpDelete("delete")]
		public IActionResult Delete(int id)
		{
			var result = _commentService.Delete(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
