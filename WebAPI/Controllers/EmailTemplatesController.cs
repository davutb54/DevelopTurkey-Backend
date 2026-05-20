using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailTemplatesController(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    [HttpGet("getall")]
    public IActionResult GetAll()
    {
        var result = _emailTemplateService.GetAll();
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("getbyid")]
    public IActionResult GetById(int id)
    {
        var result = _emailTemplateService.GetById(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("add")]
    public IActionResult Add(EmailTemplate emailTemplate)
    {
        var result = _emailTemplateService.Add(emailTemplate);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("update")]
    public IActionResult Update(EmailTemplate emailTemplate)
    {
        var result = _emailTemplateService.Update(emailTemplate);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("delete")]
    public IActionResult Delete(EmailTemplate emailTemplate)
    {
        var result = _emailTemplateService.Delete(emailTemplate);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}
