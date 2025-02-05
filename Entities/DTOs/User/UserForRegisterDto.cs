namespace Entities.DTOs.User;

public class UserForRegisterDto
{
	public required string UserName { get; set; }
	public required string Name { get; set; }
	public required string Surname { get; set; }
	public required string Email { get; set; }
	public required string Password { get; set; }
	public int CityCode { get; set; }
	public int GenderCode { get; set; }
	public bool EmailNotificationPermission { get; set; }
}