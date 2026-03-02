namespace Entities.DTOs;

public class ProblemUpdateDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CityCode { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime SendDate { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsReported { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsResolved { get; set; }
    public int InstitutionId { get; set; }
    public int ViewCount { get; set; }

    public List<int> TopicIds { get; set; }
}