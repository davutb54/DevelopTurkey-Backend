using Core.Entities;

namespace Entities.Concrete;

public class UserAgreementAcceptance : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AgreementId { get; set; }
    public string AgreementVersion { get; set; } // Denormalize — sorgu kolaylığı
    public DateTime AcceptedAt { get; set; } = DateTime.Now;
    public string? IpAddress { get; set; }       // KVKK rıza kaydı için
}
