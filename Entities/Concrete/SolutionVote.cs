using Core.Entities;

namespace Entities.Concrete;

public class SolutionVote : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SolutionId { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime VoteDate { get; set; }
}