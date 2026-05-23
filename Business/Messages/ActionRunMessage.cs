namespace Business.Workflow.Messages;

/// <summary>
/// Bir ActionRun'ı durable queue üzerinden işlemek için gönderilen mesaj.
/// Queue adı: workflow.actionrun
/// Retry politikası: 3 kez, exponential backoff (500ms → 1s → 2s)
/// </summary>
public record ActionRunMessage
{
    /// <summary>İşlenecek ActionRun'ın Guid PK'sı</summary>
    public Guid ActionRunId { get; init; }

    /// <summary>Bağlı NodeRun Guid'i</summary>
    public Guid NodeRunId { get; init; }

    /// <summary>Bağlı WorkflowRun Guid'i (log/audit için)</summary>
    public Guid RunId { get; init; }

    /// <summary>Hangi action çalıştırılacak (örn: "send_email", "ban_user")</summary>
    public string ActionCode { get; init; } = string.Empty;

    /// <summary>Action parametrelerinin JSON temsili</summary>
    public string PayloadJson { get; init; } = "{}";

    /// <summary>Kurum izolasyonu için</summary>
    public int InstitutionId { get; init; }

    /// <summary>Workflow'u tetikleyen kullanıcı</summary>
    public int TriggeredByUserId { get; init; }

    /// <summary>True ise action simüle edilir, gerçek etki yaratılmaz</summary>
    public bool IsDryRun { get; init; }

    /// <summary>Mevcut retry sayısı (0-based)</summary>
    public int AttemptNumber { get; init; }

    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
}
