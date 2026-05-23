using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Bir node run içindeki tek bir action çalışma kaydı.
/// Durable queue ile işlenir; retry sayısı burada tutulur.
/// </summary>
[Index(nameof(NodeRunId), Name = "IX_ActionRun_NodeRunId")]
[Index(nameof(Status), Name = "IX_ActionRun_Status")]
[Index(nameof(ActionCode), Name = "IX_ActionRun_ActionCode")]
public class ActionRun : IEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NodeRunId { get; set; }

    [Required, MaxLength(100)]
    public string ActionCode { get; set; } = string.Empty;

    /// <summary>1=pending, 2=running, 3=succeeded, 4=failed, 5=deadletter, 6=simulated</summary>
    public byte Status { get; set; } = 1;

    public int RetryCount { get; set; } = 0;

    [MaxLength(2000)]
    public string? LastError { get; set; }

    /// <summary>Action'a iletilen parametrelerin JSON temsili</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? PayloadJson { get; set; }

    /// <summary>Action'ın döndürdüğü sonuç/çıktı JSON</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ResultJson { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(NodeRunId))]
    public NodeRun? NodeRun { get; set; }
}

/// <summary>ActionRun durum sabitleri</summary>
public static class ActionRunStatus
{
    public const byte Pending    = 1;
    public const byte Running    = 2;
    public const byte Succeeded  = 3;
    public const byte Failed     = 4;
    public const byte DeadLetter = 5;
    public const byte Simulated  = 6;
}
