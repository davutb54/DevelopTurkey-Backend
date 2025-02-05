namespace Entities.DTOs;

public class CommentDetailDto
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public int SolutionId { get; set; }
	public int? ParentCommentId { get; set; }
	public string Text { get; set; }
	public string SenderUsername { get; set; }
	public bool SenderIsExpert { get; set; }
	public DateTime SendDate { get; set; }
}