using Core.Entities;

namespace Entities.Concrete;

public class User : IEntity
{
	public int Id { get; set; }
	public string UserName { get; set; }
	public string Name { get; set; }
	public string Surname { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public int CityCode { get; set; }
	public int Gender { get; set; }
	public bool EmailNotificationPermission { get; set; }
	public bool IsAdmin { get; set; } = false;
	public bool IsExpert { get; set; } = false;
	public bool IsReported { get; set; } = false;
	public bool IsDeleted { get; set; } = false;
	public bool IsBanned { get; set; } = false;
	public bool IsEmailVerified { get; set; } = false;
	public DateTime RegisterDate { get; set; }
	public DateTime? DeleteDate { get; set; }
}