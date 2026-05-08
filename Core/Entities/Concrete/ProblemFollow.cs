using Core.Entities;

namespace Core.Entities.Concrete;

public class ProblemFollow : IEntity {
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProblemId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
