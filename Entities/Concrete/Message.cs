using Core.Entities;

namespace Entities.Concrete;

public class Message : IEntity
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public Conversation? Conversation { get; set; }
}
