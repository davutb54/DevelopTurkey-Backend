namespace Entities.DTOs.User;

public class UserForUpdateDto
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public required string Surname { get; set; }
	public required string Email { get; set; }
	public int CityCode { get; set; }
	public int GenderCode { get; set; }
	public int? CustomHierarchyId { get; set; }
	public bool MentionNotificationEnabled { get; set; }
    public bool IsProfilePublic { get; set; }
    public bool ShowSolutions { get; set; }
    public bool ShowProblems { get; set; }
}