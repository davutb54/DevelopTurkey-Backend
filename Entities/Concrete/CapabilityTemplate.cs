using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(Name), IsUnique = true, Name = "UX_CapabilityTemplate_Name")]
[Index(nameof(Slug), IsUnique = true, Name = "UX_CapabilityTemplate_Slug")]
public class CapabilityTemplate : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe unique identifier, e.g. "standard-user"</summary>
    [MaxLength(100)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>0 = Role, 1 = Package</summary>
    public int Kind { get; set; } = 0;

    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
