using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// WorkflowDefinition'ın versiyonlanmış FlowJson içeriği.
/// Her publish yeni bir versiyon satırı oluşturur; eski versiyonlar arşivlenir.
/// </summary>
[Index(nameof(DefinitionId), nameof(Version), IsUnique = true, Name = "UX_WorkflowVersion_Definition_Version")]
[Index(nameof(Status), Name = "IX_WorkflowVersion_Status")]
public class WorkflowVersion : IEntity
{
    public int Id { get; set; }

    public int DefinitionId { get; set; }

    public int Version { get; set; }

    /// <summary>Workflow node grafiğinin JSON temsili</summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string FlowJson { get; set; } = string.Empty;

    /// <summary>1=active, 2=archived</summary>
    public byte Status { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }

    // Navigation
    [ForeignKey(nameof(DefinitionId))]
    public WorkflowDefinition? Definition { get; set; }
}
