using Core.Entities;

namespace Entities.Concrete;

public class Conversation : IEntity
{
    public int Id { get; set; }

    /// <summary>direct | group</summary>
    public string Type { get; set; } = "direct";

    /// <summary>institution | global</summary>
    public string Scope { get; set; } = "institution";

    /// <summary>Scope=institution konuşmalar için zorunlu; global ise null.</summary>
    public int? InstitutionId { get; set; }

    /// <summary>Group konuşmalara başlık; direct ise null.</summary>
    public string? Title { get; set; }

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>active | pending (destek havuzunda bekliyor) | closed</summary>
    public string Status { get; set; } = "active";

    /// <summary>support tipi konuşmalar için: general | official | admin. Diğerleri null.</summary>
    public string? SupportCategory { get; set; }

    /// <summary>Destek konuşmasını sahiplenen yetkili kullanıcı. Havuzdayken null.</summary>
    public int? AssignedToUserId { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
