using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost("add")]
    public IActionResult Add(Feedback feedback)
    {
        feedback.SendDate = DateTime.Now;
        feedback.IsRead = false;
        var result = _feedbackService.Add(feedback);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getall")]
    public IActionResult GetAll()
    {
        var result = _feedbackService.GetAllDetails();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("markasread")]
    public IActionResult MarkAsRead(int id)
    {
        var feedback = _feedbackService.GetById(id).Data;
        if (feedback != null)
        {
            feedback.IsRead = true;
            _feedbackService.Update(feedback);
            return Ok();
        }
        return BadRequest();
    }
}