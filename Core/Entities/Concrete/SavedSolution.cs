using Core.Entities;

namespace Core.Entities.Concrete;

public class SavedSolution : IEntity {
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SolutionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
