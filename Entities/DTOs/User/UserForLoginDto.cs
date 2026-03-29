namespace Entities.DTOs.User;

public class UserForLoginDto
{
	public required string UserName { get; set; }
	public required string Password { get; set; }
	public string? CaptchaToken { get; set; }
}