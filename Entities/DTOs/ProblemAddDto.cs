using Microsoft.AspNetCore.Http;

namespace Entities.DTOs;

public class ProblemAddDto
{
    public int SenderId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CityCode { get; set; }
    public int TopicId { get; set; }
    public IFormFile? Image { get; set; }
}