using Core.Entities;

namespace Core.Entities.Concrete;

public class TopicFollow : IEntity {
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TopicId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
