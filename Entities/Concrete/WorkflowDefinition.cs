using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Bir workflow kuralının tanımı. DynamicRule'un üst katmanı;
/// versiyonlanmış FlowJson'ı WorkflowVersion üzerinden tutar.
/// </summary>
[Index(nameof(TriggerEvent), nameof(InstitutionId), Name = "IX_WorkflowDefinition_Trigger_Institution")]
[Index(nameof(IsActive), Name = "IX_WorkflowDefinition_IsActive")]
public class WorkflowDefinition : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Örn: "problem.created", "solution.approved"</summary>
    [Required, MaxLength(100)]
    public string TriggerEvent { get; set; } = string.Empty;

    public int InstitutionId { get; set; }

    /// <summary>Şu an aktif olan versiyon (null = henüz versiyonlanmamış)</summary>
    public int? CurrentVersionId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }

    // Navigation
    public ICollection<WorkflowVersion>? Versions { get; set; }
}
