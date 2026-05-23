using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

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
    [RequireCapability("admin.email_template_manage")]
    public IActionResult GetAll()
    {
        var result = _emailTemplateService.GetAll();
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("getbyid")]
    [RequireCapability("admin.email_template_manage")]
    public IActionResult GetById(int id)
    {
        var result = _emailTemplateService.GetById(id);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("add")]
    [RequireCapability("admin.email_template_manage")]
    public IActionResult Add(EmailTemplate emailTemplate)
    {
        var result = _emailTemplateService.Add(emailTemplate);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("update")]
    [RequireCapability("admin.email_template_manage")]
    public IActionResult Update(EmailTemplate emailTemplate)
    {
        var result = _emailTemplateService.Update(emailTemplate);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("delete")]
    [RequireCapability("admin.email_template_manage")]
    public IActionResult Delete(EmailTemplate emailTemplate)
    {
        var result = _emailTemplateService.Delete(emailTemplate);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}
