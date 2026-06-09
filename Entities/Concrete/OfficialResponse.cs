using Core.Entities;

namespace Entities.Concrete;

public class OfficialResponse : IEntity
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int AuthorUserId { get; set; }
    public int InstitutionId { get; set; }
    public string Body { get; set; } = string.Empty;
    // acknowledged | in_progress | info | closed
    public string Status { get; set; } = "acknowledged";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
