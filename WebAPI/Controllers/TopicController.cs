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
	public class TopicController : Controller
    {
        private readonly ITopicService _topicService;

        public TopicController(ITopicService topicService)
        {
			_topicService = topicService;
        }


        [HttpGet("getbyid")]
		public IActionResult GetById(int id)
		{
			var result = _topicService.GetById(id);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpGet("getall")]
		public IActionResult GetAll()
		{
			var result = _topicService.GetAll();
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(Topic topic)
		{
			var result = _topicService.Add(topic);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpPost("update")]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(Topic topic)
		{
			var result = _topicService.Update(topic);
			return result.Success ? Ok(result) : BadRequest(result);
		}

		[HttpDelete("delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(Topic topic)
		{
			var result = _topicService.Delete(topic);
			return result.Success ? Ok(result) : BadRequest(result);
		}
	}
}
