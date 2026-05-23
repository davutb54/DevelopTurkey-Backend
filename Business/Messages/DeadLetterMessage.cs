namespace Business.Workflow.Messages;

/// <summary>
/// Maksimum retry aşıldıktan sonra dead-letter kuyruğuna düşen mesaj zarfı.
/// Queue adı: workflow.deadletter
/// </summary>
public record DeadLetterMessage
{
    public Guid? RunId { get; init; }
    public Guid? NodeRunId { get; init; }
    public Guid? ActionRunId { get; init; }

    /// <summary>Orijinal mesaj tipi (örn: "ActionRunMessage")</summary>
    public string OriginalMessageType { get; init; } = string.Empty;

    /// <summary>Dead-letter nedeni</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Son hata detayı</summary>
    public string? ErrorDetail { get; init; }

    /// <summary>Orijinal mesaj JSON payload'ı</summary>
    public string? OriginalPayloadJson { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
