namespace Entities.DTOs;

public class InstitutionPublicInfoDto
{
    public int     Id           { get; set; }
    public string  Name         { get; set; } = default!;
    public string? LogoUrl      { get; set; }
    public string? PrimaryColor { get; set; }
}
