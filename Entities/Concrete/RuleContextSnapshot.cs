using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Bir WorkflowRun'ın başlangıç anındaki RuleContext snapshot'ı.
/// Her run için tek kayıt (1-1 ilişki). Retry veya debug için context yeniden inşa edilebilir.
/// </summary>
[Index(nameof(RunId), IsUnique = true, Name = "UX_RuleContextSnapshot_RunId")]
public class RuleContextSnapshot : IEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RunId { get; set; }

    /// <summary>RuleContext nesnesinin JSON serileştirilmiş hali</summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ContextJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(RunId))]
    public WorkflowRun? Run { get; set; }
}
