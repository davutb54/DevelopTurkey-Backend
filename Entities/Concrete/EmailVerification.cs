using Core.Entities;

namespace Entities.Concrete;

public class EmailVerification : IEntity
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public int VerificationCode { get; set; }
	public bool IsVerified { get; set; } = false;
	public bool IsExpired { get; set; } = false;
	public DateTime ExpirationDate { get; set; }
	public DateTime SendDate { get; set; }
	public DateTime? VerificationDate { get; set; }

}