using Microsoft.AspNetCore.Http;

namespace Entities.DTOs;

public class SolutionAddDto
{
    public int ProblemId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<IFormFile>? Images { get; set; }
}
