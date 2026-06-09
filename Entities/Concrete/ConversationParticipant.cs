using Core.Entities;

namespace Entities.Concrete;

public class ConversationParticipant : IEntity
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int UserId { get; set; }

    /// <summary>admin | member</summary>
    public string Role { get; set; } = "member";

    /// <summary>Son okunan mesajın ID'si; null = hiç okunmadı.</summary>
    public int? LastReadMessageId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
}
