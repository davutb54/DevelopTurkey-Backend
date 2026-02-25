using Core.Entities;

namespace Entities.Concrete;

public class Solution : IEntity
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public int ProblemId { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public bool IsHighlighted { get; set; } = false;
	public bool IsReported { get; set; } = false;
	public bool IsDeleted { get; set; } = false;
	public DateTime SendDate { get; set; }
	public DateTime? DeleteDate { get; set; }
    // 0: Bekliyor (Pending), 1: Onaylandı (Approved), 2: Reddedildi (Rejected)
    public int ExpertApprovalStatus { get; set; } = 0;
}