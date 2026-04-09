namespace Entities.DTOs.User;

public class UserPublicProfileDto
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string CityName { get; set; }
    public required string Gender { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsExpert { get; set; }
    public bool IsOfficial { get; set; }
    public DateTime RegisterDate { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int InstitutionId { get; set; }
}
