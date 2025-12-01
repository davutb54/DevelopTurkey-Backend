namespace Entities.DTOs;

public class SolutionDetailDto
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public int ProblemId { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public string SenderUsername { get; set; }
	public bool SenderIsExpert { get; set; } = false;
	public string ProblemName { get; set; }
	public bool IsHighlighted { get; set; }
    public bool SenderIsOfficial { get; set; }
    public bool IsReported { get; set; }
	public bool IsDeleted { get; set; }
	public DateTime SendDate { get; set; }

}