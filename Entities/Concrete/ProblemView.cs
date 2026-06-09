using Core.Entities;

namespace Entities.Concrete;

public class ProblemView : IEntity
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int? UserId { get; set; }
    public int InstitutionId { get; set; }
    public DateTime ViewedAt { get; set; }
}
