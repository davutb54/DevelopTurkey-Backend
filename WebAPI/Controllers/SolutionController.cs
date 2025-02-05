using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SolutionController : Controller
	{
		private readonly ISolutionService _solutionService = new SolutionManager();

		[HttpGet("getbyid")]
		public IActionResult GetById(int id)
		{
			var result = _solutionService.GetById(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getall")]
		public IActionResult GetAll()
		{
			var result = _solutionService.GetAll();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbyproblem")]
		public IActionResult GetByProblem(int problemId)
		{
			var result = _solutionService.GetByProblem(problemId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getbysender")]
		public IActionResult GetBySender(int senderId)
		{
			var result = _solutionService.GetBySender(senderId);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getishighlighted")]
		public IActionResult GetIsHighlighted()
		{
			var result = _solutionService.GetIsHighlighted();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("add")]
		public IActionResult Add(Solution solution)
		{
			var result = _solutionService.Add(solution);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
		public IActionResult Update(Solution solution)
		{
			var result = _solutionService.Update(solution);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpDelete("delete")]
		public IActionResult Delete(int id)
		{
			var result = _solutionService.Delete(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
