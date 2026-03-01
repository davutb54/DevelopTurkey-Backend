using Microsoft.AspNetCore.Http;
namespace Entities.DTOs;

public class InstitutionAddDto
{
    public string Name { get; set; }
    public string Domain { get; set; }
    public IFormFile? Logo { get; set; }
    public string? PrimaryColor { get; set; }
    public bool Status { get; set; }
}