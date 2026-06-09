using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(UserId), nameof(TemplateId))]
public class UserAppliedTemplate : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TemplateId { get; set; }
    public int TemplateVersionId { get; set; }
    public int? InstitutionId { get; set; }
    public DateTime AppliedAt { get; set; }
    public int AppliedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int? RevokedBy { get; set; }
}
