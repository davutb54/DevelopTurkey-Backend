using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entities.Concrete;

/// <summary>
/// Maksimum retry aşıldıktan sonra dead-letter queue'ya düşen mesajlar.
/// Tüm Id alanları nullable — hata hangi katmanda oluştuğuna bağlı olarak kısmen dolu olabilir.
/// </summary>
[Index(nameof(RunId), Name = "IX_WorkflowDeadLetter_RunId")]
[Index(nameof(CreatedAt), Name = "IX_WorkflowDeadLetter_CreatedAt")]
public class WorkflowDeadLetter : IEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Hangi run'dan geldiği (varsa)</summary>
    public Guid? RunId { get; set; }

    /// <summary>Hangi node run'dan geldiği (varsa)</summary>
    public Guid? NodeRunId { get; set; }

    /// <summary>Hangi action run'dan geldiği (varsa)</summary>
    public Guid? ActionRunId { get; set; }

    /// <summary>Dead-letter sebebi (örn: "max_retry_exceeded", "non_retryable_error")</summary>
    [Required, MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Orijinal mesaj payload'ı — yeniden işleme için kullanılabilir</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? PayloadJson { get; set; }

    /// <summary>Son hata detayı</summary>
    [MaxLength(4000)]
    public string? ErrorDetail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Manuel müdahale ile yeniden kuyruğa alındı mı?</summary>
    public bool IsRequeued { get; set; } = false;

    public DateTime? RequeuedAt { get; set; }
    public int? RequeuedByUserId { get; set; }
}
