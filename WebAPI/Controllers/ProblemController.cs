using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProblemController : Controller
	{
		private readonly IProblemService _problemService = new ProblemManager();

		[HttpGet("getbyid")]
		public IActionResult GetById(int id)
		{
			var result = _problemService.GetById(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getall")]
		public IActionResult GetAll()
		{
			var result = _problemService.GetAll();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbytopic")]
		public IActionResult GetByTopic(int topicId)
		{
			var result = _problemService.GetByTopic(topicId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbysender")]
		public IActionResult GetBySender(int senderId)
		{
			var result = _problemService.GetBySender(senderId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getishighlighted")]
		public IActionResult GetIsHighlighted()
		{
			var result = _problemService.GetIsHighlighted();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("add")]
		public IActionResult Add(Problem problem)
		{
			var result = _problemService.Add(problem);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
		public IActionResult Update(Problem problem)
		{
			var result = _problemService.Update(problem);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpDelete("delete")]
		public IActionResult Delete(int id)
		{
			var result = _problemService.Delete(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
