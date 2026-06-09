using Microsoft.AspNetCore.Http;
namespace Entities.DTOs;

public class InstitutionUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Subtitle { get; set; } = "Özel Kurum Ağı";
    public string Domain { get; set; }
    public string? Subdomain { get; set; }
    public IFormFile? Logo { get; set; }
    public string? ExistingLogoUrl { get; set; }
    public string? PrimaryColor { get; set; }

    public string CustomFieldsJson { get; set; }
    public string? CustomHierarchyLabel { get; set; }
    public string? CustomHierarchyJson { get; set; }

    public bool Status { get; set; }
}