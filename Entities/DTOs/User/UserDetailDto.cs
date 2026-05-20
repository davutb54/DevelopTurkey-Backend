namespace Entities.DTOs.User;

public class UserDetailDto
{
	public int Id { get; set; }
	public required string UserName { get; set; }
	public required string Name { get; set; }
	public required string Surname { get; set; }
	public required string Email { get; set; }
	public required string CityName { get; set; }
	public required string Gender { get; set; }
    public int CityCode { get; set; }
    public int GenderCode { get; set; }
	public bool EmailNotificationPermission { get; set; }
	public bool MentionNotificationEnabled { get; set; }
    public bool IsProfilePublic { get; set; }
    public bool ShowSolutions { get; set; }
    public bool ShowProblems { get; set; }
	public bool IsAdmin { get; set; }
	public bool IsExpert { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsReported { get; set; }
	public bool IsDeleted { get; set; }
	public bool IsBanned { get; set; }
	public bool IsEmailVerified { get; set; }
	public string AuthType { get; set; }
	public bool HasPassword { get; set; }
	public DateTime RegisterDate { get; set; }
	public DateTime? DeleteDate { get; set; }
	public DateTime? LastUsernameChangeDate { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int InstitutionId { get; set; }
    public int? CustomHierarchyId { get; set; }
}