using Business.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Utilities.Context;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly IClientContext _clientContext;

    public FeedbackController(IFeedbackService feedbackService, IClientContext clientContext)
    {
        _feedbackService = feedbackService;
        _clientContext = clientContext;
    }

    [HttpPost("add")]
    public IActionResult Add(Feedback feedback)
    {
        feedback.UserId = _clientContext.GetUserId() ?? 0;
        feedback.SendDate = DateTime.Now;
        feedback.IsRead = false;
        var result = _feedbackService.Add(feedback);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getall")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAll()
    {
        var result = _feedbackService.GetAllDetails();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getallpaged")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAllPaged([FromQuery] FeedbackFilterDto filter)
    {
        var result = _feedbackService.GetAllDetailsPaged(filter);
        if (!result.Success) return BadRequest(result);

        return Ok(new
        {
            success = true,
            data = result.Data.Items,
            totalCount = result.Data.TotalCount,
            page = filter.Page,
            pageSize = filter.PageSize,
            totalPages = (int)Math.Ceiling((double)result.Data.TotalCount / filter.PageSize)
        });
    }

    [HttpPost("markasread")]
    [Authorize(Roles = "Admin")]
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