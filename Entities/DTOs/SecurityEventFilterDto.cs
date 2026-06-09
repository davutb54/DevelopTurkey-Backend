namespace Entities.DTOs;

public class SecurityEventFilterDto
{
    public string? IpAddress { get; set; }
    public int? UserId { get; set; }
    public string? EventType { get; set; }
    public string? Severity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
