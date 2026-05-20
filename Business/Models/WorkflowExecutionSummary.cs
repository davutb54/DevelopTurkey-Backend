using System.Text.Json.Serialization;

namespace Business.Models;

/// <summary>
/// WorkflowInterpreterService.ExecuteWorkflowAsync sonucu.
/// İ5: anonymous object yerine strongly-typed record — handler'da
/// Serialize→Deserialize çift dönüşümünü ortadan kaldırır.
///
/// JsonPropertyName: anonymous object eski camelCase JSON şemasıyla
/// backward-compatible kalmak için camelCase serialize edilir
/// (UI ve harness tarafında lowercase property bekleniyor).
/// </summary>
public sealed class WorkflowExecutionSummary
{
    /// <summary>Workflow graph'taki toplam node sayısı.</summary>
    [JsonPropertyName("nodeCount")]
    public int NodeCount { get; init; }

    /// <summary>Bu çalıştırmada gerçekten ziyaret edilen node sayısı.</summary>
    [JsonPropertyName("visitedNodeCount")]
    public int VisitedNodeCount { get; init; }

    /// <summary>Node ziyaret/sonuç sırasının kronolojik kayıtları (UI trace paneli için).</summary>
    [JsonPropertyName("trace")]
    public List<string> Trace { get; init; } = new();

    /// <summary>Yapısal hata mesajları (B4 fix). Boş ise success, doluysa partial.</summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; init; } = new();

    /// <summary>Errors.Count > 0 kısayolu. Status hesabında kullanılır.</summary>
    [JsonPropertyName("hasErrors")]
    public bool HasErrors { get; init; }

    /// <summary>DFS sırasında üretilen son sonuç (CSharpNode return value veya action result).</summary>
    [JsonPropertyName("lastResult")]
    public object? LastResult { get; init; }
}
