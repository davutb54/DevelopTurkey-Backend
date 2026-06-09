namespace Entities.DTOs;

public class OfficialResponseDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int AuthorUserId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string? AuthorImageUrl { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "acknowledged";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<UserTitleDto> AuthorTitles { get; set; } = new();
}

public class OfficialResponseAddDto
{
    public int ProblemId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "acknowledged";
}

public class OfficialResponseUpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Body { get; set; }
}
