using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Bir workflow run içindeki tek bir düğüm (node) çalışma kaydı.
/// </summary>
[Index(nameof(RunId), Name = "IX_NodeRun_RunId")]
[Index(nameof(Status), Name = "IX_NodeRun_Status")]
[Index(nameof(RunId), nameof(NodeId), Name = "IX_NodeRun_RunId_NodeId")]
public class NodeRun : IEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RunId { get; set; }

    /// <summary>FlowJson içindeki node kimliği (React Flow node id)</summary>
    [Required, MaxLength(100)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>triggerNode | conditionNode | actionNode | csharpNode</summary>
    [Required, MaxLength(50)]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>1=pending, 2=running, 3=succeeded, 4=failed, 5=skipped</summary>
    public byte Status { get; set; } = 1;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    // Navigation
    [ForeignKey(nameof(RunId))]
    public WorkflowRun? Run { get; set; }

    public ICollection<ActionRun>? ActionRuns { get; set; }
}

/// <summary>NodeRun durum sabitleri</summary>
public static class NodeRunStatus
{
    public const byte Pending   = 1;
    public const byte Running   = 2;
    public const byte Succeeded = 3;
    public const byte Failed    = 4;
    public const byte Skipped   = 5;
}
