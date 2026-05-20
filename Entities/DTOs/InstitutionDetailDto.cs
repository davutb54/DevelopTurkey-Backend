namespace Entities.DTOs;

public class InstitutionDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Subtitle { get; set; }
    public string Domain { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }

    public string CustomFieldsJson { get; set; }

    public bool Status { get; set; }
}
