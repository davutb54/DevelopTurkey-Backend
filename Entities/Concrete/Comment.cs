using Core.Entities;

namespace Entities.Concrete;

public class Comment : IEntity
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public int SolutionId { get; set; }
	public int? ParentCommentId { get; set; }
	public string Text { get; set; }
	public DateTime SendDate { get; set; }
	public DateTime? DeleteDate { get; set; }
	public bool IsDeleted { get; set; }
}