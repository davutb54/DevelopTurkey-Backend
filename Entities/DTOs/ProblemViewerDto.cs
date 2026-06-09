namespace Entities.DTOs;

public class ProblemViewerDto
{
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime ViewedAt { get; set; }
}

public class ProblemUpvoterDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProblemParticipantDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public string Role { get; set; } = "";
}

public class SolutionVoterDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime VoteDate { get; set; }
}
