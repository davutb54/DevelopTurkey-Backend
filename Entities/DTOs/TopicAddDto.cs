using Microsoft.AspNetCore.Http;
namespace Entities.DTOs;

public class TopicAddDto
{
    public string Name { get; set; }
    public IFormFile? Image { get; set; }
    public bool Status { get; set; } = true;
}