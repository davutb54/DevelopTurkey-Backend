namespace Entities.DTOs;

public class LogFilterDto
{
    public string? Type { get; set; }
    public string? SearchText { get; set; }
    public DateTime? StartDate { get; set; } 
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}