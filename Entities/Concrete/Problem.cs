using Core.Entities;

namespace Entities.Concrete;

public class Problem : IEntity
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public int CityCode { get; set; }
	public int TopicId { get; set; }
	public bool IsHighlighted { get; set; } = false;
	public bool IsReported { get; set; } = false;
	public bool IsDeleted { get; set; } = false;
	public DateTime SendDate { get; set; }
	public DateTime? DeleteDate { get; set; }
    public string? ImageUrl { get; set; }

}