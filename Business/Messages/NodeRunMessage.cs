namespace Business.Workflow.Messages;

/// <summary>
/// Bir NodeRun'ı durable queue üzerinden işlemek için gönderilen mesaj.
/// Queue adı: workflow.noderun
/// </summary>
public record NodeRunMessage
{
    /// <summary>İşlenecek NodeRun'ın Guid PK'sı</summary>
    public Guid NodeRunId { get; init; }

    /// <summary>Bağlı WorkflowRun Guid'i (log/audit için)</summary>
    public Guid RunId { get; init; }

    /// <summary>Workflow tanımı numarası</summary>
    public int DefinitionId { get; init; }

    /// <summary>FlowJson'daki node kimliği</summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>triggerNode | conditionNode | actionNode | csharpNode</summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>Kurum izolasyonu için</summary>
    public int InstitutionId { get; init; }

    /// <summary>True ise action'lar simüle edilir, gerçek etki yaratılmaz</summary>
    public bool IsDryRun { get; init; }

    /// <summary>Mesaj oluşturulma zamanı (retry/delay hesaplaması için)</summary>
    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
}
