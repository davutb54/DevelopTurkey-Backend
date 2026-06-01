namespace Entities.DTOs;

public record TestRunRequestDto
{
    public string? TriggerEvent { get; init; }
    public int? SampleProblemId { get; init; }
    public int? SampleUserId { get; init; }
}
