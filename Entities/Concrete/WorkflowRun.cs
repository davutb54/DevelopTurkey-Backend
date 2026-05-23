using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Bir workflow definition'ının tek bir çalışma kaydı.
/// PK = Guid; idempotency key = (EventId, DefinitionId, VersionId).
/// </summary>
[Index(nameof(EventId), nameof(DefinitionId), nameof(VersionId), IsUnique = true, Name = "UX_WorkflowRun_Idempotency")]
[Index(nameof(DefinitionId), Name = "IX_WorkflowRun_DefinitionId")]
[Index(nameof(InstitutionId), Name = "IX_WorkflowRun_InstitutionId")]
[Index(nameof(Status), Name = "IX_WorkflowRun_Status")]
[Index(nameof(StartedAt), Name = "IX_WorkflowRun_StartedAt")]
public class WorkflowRun : IEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int DefinitionId { get; set; }
    public int VersionId { get; set; }

    [Required, MaxLength(100)]
    public string TriggerEvent { get; set; } = string.Empty;

    public int InstitutionId { get; set; }
    public int TriggeredByUserId { get; set; }

    /// <summary>Dış event'in benzersiz tanımlayıcısı — idempotency için kullanılır</summary>
    public Guid EventId { get; set; }

    /// <summary>1=pending, 2=running, 3=succeeded, 4=partial, 5=failed, 6=canceled, 7=timeout</summary>
    public byte Status { get; set; } = 1;

    /// <summary>Dry-run modu: side-effect actionlar simüle edilir, gerçek etki yaratmaz</summary>
    public bool IsDryRun { get; set; } = false;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public long? DurationMs { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    // Navigation
    public ICollection<NodeRun>? NodeRuns { get; set; }
    public RuleContextSnapshot? ContextSnapshot { get; set; }
}

/// <summary>WorkflowRun durum sabitleri</summary>
public static class WorkflowRunStatus
{
    public const byte Pending   = 1;
    public const byte Running   = 2;
    public const byte Succeeded = 3;
    public const byte Partial   = 4;
    public const byte Failed    = 5;
    public const byte Canceled  = 6;
    public const byte Timeout   = 7;
}
