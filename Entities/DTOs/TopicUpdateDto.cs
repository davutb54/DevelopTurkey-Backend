using Microsoft.AspNetCore.Http;

namespace Entities.DTOs;

public class TopicUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public IFormFile? Image { get; set; }
    public string? ExistingImageName { get; set; }
    public bool Status { get; set; } = true;
}