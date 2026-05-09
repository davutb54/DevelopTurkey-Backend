using Core.Entities;

namespace Entities.Concrete;

public class ProblemUpvote : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProblemId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
