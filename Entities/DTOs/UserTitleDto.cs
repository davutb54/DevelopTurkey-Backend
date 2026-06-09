namespace Entities.DTOs;

public class UserTitleDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = "custom";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsVisible { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class UserTitleAddDto
{
    public int UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = "custom";
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
