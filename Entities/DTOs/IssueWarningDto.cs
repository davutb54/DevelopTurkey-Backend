namespace Entities.DTOs;

public class IssueWarningDto
{
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Severity { get; set; }
}
