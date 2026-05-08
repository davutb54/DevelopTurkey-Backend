using Microsoft.AspNetCore.Http;

namespace Entities.DTOs;

public class ProblemAddDto
{
    public int SenderId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CityCode { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<int> TopicIds { get; set; }
    public IFormFile? Image { get; set; }

    public string? SolutionTitle { get; set; }
    public string? SolutionDescription { get; set; }
}