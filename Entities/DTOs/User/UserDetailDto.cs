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
	public bool EmailNotificationPermission { get; set; }
	public bool IsAdmin { get; set; }
	public bool IsExpert { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsReported { get; set; }
	public bool IsDeleted { get; set; }
	public bool IsBanned { get; set; }
	public bool IsEmailVerified { get; set; }
	public DateTime RegisterDate { get; set; }
	public DateTime? DeleteDate { get; set; }
}