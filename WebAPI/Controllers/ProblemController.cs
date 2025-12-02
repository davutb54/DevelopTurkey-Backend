using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProblemController : Controller
    {
        private readonly IProblemService _problemService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProblemController(IProblemService problemService, IWebHostEnvironment webHostEnvironment)
        {
            _problemService = problemService;
            _webHostEnvironment = webHostEnvironment;
        }

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
        public IActionResult Add([FromForm] ProblemAddDto problemAddDto)
        {
            string imagePath = null;
            if (problemAddDto.Image != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "problems");
                imagePath = Core.Utilities.Helpers.FileHelper.FileHelper.Add(problemAddDto.Image, uploadPath);
            }

            var problem = new Problem
            {
                SenderId = problemAddDto.SenderId,
                Title = problemAddDto.Title,
                Description = problemAddDto.Description,
                CityCode = problemAddDto.CityCode,
                TopicId = problemAddDto.TopicId,
                ImageUrl = imagePath,
                SendDate = DateTime.Now,
                IsHighlighted = false,
                IsReported = false,
                IsDeleted = false
            };

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
