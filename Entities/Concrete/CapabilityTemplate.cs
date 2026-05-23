using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(Name), IsUnique = true, Name = "UX_CapabilityTemplate_Name")]
public class CapabilityTemplate : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
