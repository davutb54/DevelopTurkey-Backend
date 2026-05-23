using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicController : Controller
    {
        private readonly ITopicService _topicService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TopicController(ITopicService topicService, IWebHostEnvironment webHostEnvironment)
        {
            _topicService = topicService;
            _webHostEnvironment = webHostEnvironment;
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
            // institutionId is handled by IClientContext inside TopicManager
            var result = _topicService.GetAll();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("getallactive")]
        [AllowAnonymous]
        public IActionResult GetAllActive()
        {
            // institutionId is handled by IClientContext inside TopicManager
            var result = _topicService.GetAll();
            if (result.Success)
            {
                var activeTopics = result.Data.Where(t => t.Status == true).ToList();
                return Ok(new SuccessDataResult<List<Topic>>(activeTopics));
            }
            return BadRequest();
        }

        [HttpPost("add")]
        [RequireCapability("moderation.topic_create")]
        public IActionResult Add([FromForm] TopicAddDto topicAddDto)
        {
            string imagePath = "default.png";

            if (topicAddDto.Image != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "topics");
                try
                {
                    imagePath = Core.Utilities.Helpers.FileHelper.FileHelper.Add(topicAddDto.Image, uploadPath);
                }
                catch (Exception ex)
                {
                    return BadRequest("Dosya yüklenirken hata oluştu: " + ex.Message);
                }
            }

            var topic = new Topic { Name = topicAddDto.Name, ImageName = imagePath, Status = topicAddDto.Status, InstitutionId = topicAddDto.InstitutionId };
            var result = _topicService.Add(topic);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        [RequireCapability("moderation.topic_update")]
        public IActionResult Update([FromForm] TopicUpdateDto dto)
        {
            var existingTopic = _topicService.GetById(dto.Id).Data;
            if (existingTopic == null) return BadRequest("Kategori bulunamadı");

            string imageName = dto.ExistingImageName ?? existingTopic.ImageName;

            if (dto.Image != null)
            {
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "topics");
                try
                {
                    imageName = Core.Utilities.Helpers.FileHelper.FileHelper.Add(dto.Image, uploadPath);
                }
                catch (Exception ex) { return BadRequest(ex.Message); }
            }

            existingTopic.Name = dto.Name;
            existingTopic.ImageName = imageName;
            existingTopic.Status = dto.Status;

            var result = _topicService.Update(existingTopic);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete")]
        [RequireCapability("moderation.topic_delete")]
        public IActionResult Delete(int id)
        {
            var topicResult = _topicService.GetById(id);
            if (!topicResult.Success) return BadRequest("Kategori bulunamadı");

            var result = _topicService.Delete(topicResult.Data);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
