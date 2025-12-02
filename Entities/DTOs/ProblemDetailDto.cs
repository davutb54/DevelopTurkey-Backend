namespace Entities.DTOs;

public class ProblemDetailDto
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public string TopicName { get; set; }
	public string SenderUsername { get; set; }
	public bool SenderIsExpert { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public int CityCode { get; set; }
	public int TopicId { get; set; }
	public string CityName { get; set; }
	public bool IsHighlighted { get; set; }
	public bool IsReported { get; set; }
	public bool IsDeleted { get; set; }
	public DateTime SendDate { get; set; }
    public string? ImageUrl { get; set; }
}