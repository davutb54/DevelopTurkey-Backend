namespace Entities.DTOs;

public class ProblemFilterDto
{
    public int? CityCode { get; set; }
    public int? TopicId { get; set; }
    public string? SearchText { get; set; }
    public bool? IsOfficialResponse { get; set; }
    public int Page { get; set; } = 1;    
    public int PageSize { get; set; } = 10;
}