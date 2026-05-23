using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

[Index(nameof(Code), IsUnique = true, Name = "UX_Capability_Code")]
public class Capability : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; set; }

    public bool IsSystem { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
