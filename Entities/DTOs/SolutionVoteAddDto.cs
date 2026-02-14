namespace Entities.DTOs;

public class SolutionVoteAddDto
{
    public int SolutionId { get; set; }
    public int UserId { get; set; }
    public bool IsUpvote { get; set; }
}