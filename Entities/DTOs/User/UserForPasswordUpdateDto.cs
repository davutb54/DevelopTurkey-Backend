namespace Entities.DTOs.User;

public class UserForPasswordUpdateDto
{
	public int Id { get; set; }
	public required string OldPassword { get; set; }
	public required string NewPassword { get; set; }
}