using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Business.Abstract;
using Business.Models;
using Core.Utilities.Results;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// FlowJson içindeki node ağacını DFS ile gezerek workflow adımlarını çalıştırır.
/// </summary>
public class WorkflowInterpreterManager : IWorkflowInterpreterService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IRuleExecutionService _ruleExecutionService;
    private readonly IWorkflowActionDispatcher _actionDispatcher;
    private readonly ILogger<WorkflowInterpreterManager> _logger;

    public WorkflowInterpreterManager(
        IRuleExecutionService ruleExecutionService,
        IWorkflowActionDispatcher actionDispatcher,
        ILogger<WorkflowInterpreterManager> logger)
    {
        _ruleExecutionService = ruleExecutionService;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async Task<IDataResult<object?>> ExecuteWorkflowAsync(string flowJson, RuleContext context)
    {
        if (string.IsNullOrWhiteSpace(flowJson))
        {
            return new ErrorDataResult<object?>(null, "FlowJson boş olamaz.");
        }

        if (context == null)
        {
            return new ErrorDataResult<object?>(null, "RuleContext boş olamaz.");
        }

        try
        {
            var graph = JsonSerializer.Deserialize<WorkflowGraphDefinition>(flowJson, JsonOptions);
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
            {
                return new ErrorDataResult<object?>(null, "Workflow içinde çalıştırılacak node bulunamadı.");
            }

            var nodes = graph.Nodes
                .Where(node => !string.IsNullOrWhiteSpace(node.Id))
                .ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);

            var outgoingEdges = graph.Edges
                .Where(edge => !string.IsNullOrWhiteSpace(edge.Source) && !string.IsNullOrWhiteSpace(edge.Target))
                .GroupBy(edge => edge.Source, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var incomingEdges = graph.Edges
                .Where(edge => !string.IsNullOrWhiteSpace(edge.Source) && !string.IsNullOrWhiteSpace(edge.Target))
                .GroupBy(edge => edge.Target, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var rootNodes = nodes.Values
                .Where(node => IsTriggerNode(node.Type))
                .ToList();

            if (rootNodes.Count == 0)
            {
                rootNodes = nodes.Values
                    .Where(node => !incomingEdges.ContainsKey(node.Id))
                    .ToList();
            }

            if (rootNodes.Count == 0)
            {
                rootNodes = nodes.Values.Take(1).ToList();
            }

            var visitedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trace = new List<string>();
            // B4 fix: trace string'i içinde hata aramak yerine yapısal liste.
            // Action/CSharp node hata verdiğinde hem trace hem errors'a eklenir.
            var errors = new List<string>();
            object? lastResult = null;

            foreach (var rootNode in rootNodes)
            {
                var traversalResult = await TraverseAsync(
                    rootNode,
                    nodes,
                    outgoingEdges,
                    visitedNodes,
                    context,
                    trace,
                    errors);

                if (traversalResult is not null)
                {
                    lastResult = traversalResult;
                }
            }

            // İ5: strongly-typed record. WorkflowEventHandler'da artık çift JSON
            // serialize/deserialize dönüşümüne gerek yok.
            var executionSummary = new WorkflowExecutionSummary
            {
                NodeCount = nodes.Count,
                VisitedNodeCount = visitedNodes.Count,
                Trace = trace,
                Errors = errors,
                HasErrors = errors.Count > 0,
                LastResult = lastResult
            };

            return new SuccessDataResult<object?>(executionSummary, "Workflow başarıyla çalıştırıldı.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Workflow JSON ayrıştırılamadı.");
            return new ErrorDataResult<object?>(null, $"Workflow JSON ayrıştırma hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow çalıştırılırken beklenmeyen hata oluştu.");
            return new ErrorDataResult<object?>(null, $"Workflow çalıştırma hatası: {ex.Message}");
        }
    }

    private async Task<object?> TraverseAsync(
        WorkflowNodeDefinition node,
        IReadOnlyDictionary<string, WorkflowNodeDefinition> nodes,
        IReadOnlyDictionary<string, List<WorkflowEdgeDefinition>> outgoingEdges,
        ISet<string> visitedNodes,
        RuleContext context,
        ICollection<string> trace,
        ICollection<string> errors)
    {
        if (!visitedNodes.Add(node.Id))
        {
            trace.Add($"Node atlandı (zaten ziyaret edildi): {node.Id}");
            return null;
        }

        trace.Add($"Node çalıştırılıyor: {node.Id} ({node.Type})");

        var nodeType = Normalize(node.Type);
        object? result = null;

        switch (nodeType)
        {
            case "conditionnode":
            {
                var conditionResult = EvaluateCondition(node, context);
                trace.Add($"Condition sonucu: {conditionResult}");

                var branchHandle = conditionResult ? "yes" : "no";
                var nextNodes = ResolveNextNodes(node.Id, nodes, outgoingEdges, branchHandle);
                foreach (var nextNode in nextNodes)
                {
                    var nextResult = await TraverseAsync(nextNode, nodes, outgoingEdges, visitedNodes, context, trace, errors);
                    if (nextResult is not null)
                    {
                        result = nextResult;
                    }
                }

                return conditionResult;
            }

            case "csharpnode":
            {
                var csharpCode = GetNodeString(node, "code");
                var executionResult = await _ruleExecutionService.ExecuteCSharpNodeAsync(csharpCode ?? string.Empty, context);
                if (executionResult.Success)
                {
                    trace.Add("CSharpNode başarıyla çalıştı.");
                    result = executionResult.Data;
                }
                else
                {
                    var msg = executionResult.Message ?? "(no message)";
                    trace.Add($"CSharpNode hata verdi: {msg}");
                    errors.Add($"CSharpNode[{node.Id}]: {msg}");
                }

                break;
            }

            case "actionnode":
            {
                var actionResult = await DispatchActionNodeAsync(node, context);
                if (actionResult.Success)
                {
                    trace.Add($"ActionNode devredildi: {GetNodeString(node, "action")}");
                    result = actionResult.Data;
                }
                else
                {
                    var actionCode = GetNodeString(node, "action") ?? "(unknown)";
                    var msg = actionResult.Message ?? "(no message)";
                    trace.Add($"ActionNode hata verdi: {msg}");
                    errors.Add($"ActionNode[{node.Id}:{actionCode}]: {msg}");
                }

                break;
            }

            case "triggernode":
                // Trigger node bir başlangıç işaretidir — yan etkisi yoktur,
                // traversal bir sonraki node'lara doğal şekilde devam eder.
                trace.Add($"Trigger node — başlangıç noktası ({node.Id})");
                break;

            default:
                trace.Add($"Desteklenmeyen node tipi atlandı: {node.Type}");
                break;
        }

        var nextNodesWithoutBranch = ResolveNextNodes(node.Id, nodes, outgoingEdges, null);
        foreach (var nextNode in nextNodesWithoutBranch)
        {
            var nextResult = await TraverseAsync(nextNode, nodes, outgoingEdges, visitedNodes, context, trace, errors);
            if (nextResult is not null)
            {
                result = nextResult;
            }
        }

        return result;
    }

    private Task<IDataResult<object?>> DispatchActionNodeAsync(WorkflowNodeDefinition node, RuleContext context)
    {
        var actionCode = GetNodeString(node, "action") ?? string.Empty;
        var parameters = ExtractParameters(node);

        _logger.LogInformation(
            "[WorkflowInterpreter] ActionNode dispatching. Action={ActionCode}, NodeId={NodeId}, UserId={UserId}",
            actionCode,
            node.Id,
            context.SystemUserId);

        return _actionDispatcher.DispatchAsync(actionCode, parameters, context);
    }

    private bool EvaluateCondition(WorkflowNodeDefinition node, RuleContext context)
    {
        var field = GetNodeString(node, "field");
        var operatorValue = GetNodeString(node, "operator");
        var expectedValue = GetNodeString(node, "value");

        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(operatorValue))
        {
            return false;
        }

        var actualValue = ResolveContextValue(context, field);

        // ── Variable Resolver: {{FieldPath}} desenini algıla ──
        // Eğer expectedValue "{{SystemUserId}}" gibi bir desenle geliyorsa
        // bu bir statik değer değil, başka bir context field referansıdır.
        var resolvedExpectedValue = ResolveExpectedValue(expectedValue, context);

        return operatorValue.Trim().ToLowerInvariant() switch
        {
            "eq"         => CompareEqual(actualValue, resolvedExpectedValue),
            "neq"        => !CompareEqual(actualValue, resolvedExpectedValue),
            "gt"         => CompareGreaterThan(actualValue, resolvedExpectedValue),
            "gte"        => CompareGreaterThan(actualValue, resolvedExpectedValue) || CompareEqual(actualValue, resolvedExpectedValue),
            "lt"         => CompareLessThan(actualValue, resolvedExpectedValue),
            "lte"        => CompareLessThan(actualValue, resolvedExpectedValue) || CompareEqual(actualValue, resolvedExpectedValue),
            "contains"   => CompareStringOp(actualValue, resolvedExpectedValue, (a, e) => a.Contains(e, StringComparison.OrdinalIgnoreCase)),
            "startswith" => CompareStringOp(actualValue, resolvedExpectedValue, (a, e) => a.StartsWith(e, StringComparison.OrdinalIgnoreCase)),
            "endswith"   => CompareStringOp(actualValue, resolvedExpectedValue, (a, e) => a.EndsWith(e, StringComparison.OrdinalIgnoreCase)),
            "in"         => CompareInOp(actualValue, resolvedExpectedValue),
            _            => false
        };
    }

    /// <summary>
    /// Beklenen değeri çözümler: Eğer {{FieldPath}} deseni ile geliyorsa
    /// RuleContext'ten o field'ın runtime değerini alıp string olarak döner.
    /// Statik değerler aynen geçer.
    /// </summary>
    private static string? ResolveExpectedValue(string? expectedValue, RuleContext context)
    {
        if (string.IsNullOrWhiteSpace(expectedValue))
            return expectedValue;

        var trimmed = expectedValue.Trim();
        if (trimmed.StartsWith("{{") && trimmed.EndsWith("}}"))
        {
            var fieldPath = trimmed[2..^2].Trim(); // "{{SystemUserId}}" → "SystemUserId"
            if (string.IsNullOrWhiteSpace(fieldPath))
                return expectedValue;

            var resolved = ResolveContextValue(context, fieldPath);
            return resolved is null ? null : Convert.ToString(resolved, CultureInfo.InvariantCulture);
        }

        return expectedValue;
    }

    private static bool CompareStringOp(object actualValue, string? expectedValue, Func<string, string, bool> comparer)
    {
        if (actualValue is null || expectedValue is null) return false;
        var actualStr = Convert.ToString(actualValue, CultureInfo.InvariantCulture);
        if (actualStr is null) return false;
        return comparer(actualStr, expectedValue);
    }

    private static bool CompareInOp(object actualValue, string? expectedValue)
    {
        // Beklenen değer "Admin,SuperAdmin" gibi virgülle ayrılmış liste.
        // Her token için CompareEqual kullanılır (sayı/string/boolean dönüşüm desteği).
        if (expectedValue is null) return false;
        var tokens = expectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (CompareEqual(actualValue, token)) return true;
        }
        return false;
    }

    private static bool CompareEqual(object actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return expectedValue is null;
        }

        if (actualValue is bool actualBool && bool.TryParse(expectedValue, out var expectedBool))
        {
            return actualBool == expectedBool;
        }

        if (TryConvertToDecimal(actualValue, out var actualDecimal) && TryParseDecimal(expectedValue, out var expectedDecimal))
        {
            return actualDecimal == expectedDecimal;
        }

        if (actualValue is DateTime actualDateTime && DateTime.TryParse(expectedValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expectedDateTime))
        {
            return actualDateTime == expectedDateTime;
        }

        return string.Equals(Convert.ToString(actualValue, CultureInfo.InvariantCulture), expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CompareGreaterThan(object actualValue, string? expectedValue)
    {
        if (TryConvertToDecimal(actualValue, out var actualDecimal) && TryParseDecimal(expectedValue, out var expectedDecimal))
        {
            return actualDecimal > expectedDecimal;
        }

        if (actualValue is DateTime actualDateTime && DateTime.TryParse(expectedValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expectedDateTime))
        {
            return actualDateTime > expectedDateTime;
        }

        return false;
    }

    private static bool CompareLessThan(object actualValue, string? expectedValue)
    {
        if (TryConvertToDecimal(actualValue, out var actualDecimal) && TryParseDecimal(expectedValue, out var expectedDecimal))
        {
            return actualDecimal < expectedDecimal;
        }

        if (actualValue is DateTime actualDateTime && DateTime.TryParse(expectedValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expectedDateTime))
        {
            return actualDateTime < expectedDateTime;
        }

        return false;
    }

    private static bool TryConvertToDecimal(object value, out decimal result)
    {
        switch (value)
        {
            case byte byteValue:
                result = byteValue;
                return true;
            case sbyte sbyteValue:
                result = sbyteValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case ushort ushortValue:
                result = ushortValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case uint uintValue:
                result = uintValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case ulong ulongValue:
                result = ulongValue;
                return true;
            case float floatValue:
                result = (decimal)floatValue;
                return true;
            case double doubleValue:
                result = (decimal)doubleValue;
                return true;
            case decimal decimalValue:
                result = decimalValue;
                return true;
            case string stringValue:
                return decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            default:
                return decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private static object? ResolveContextValue(RuleContext context, string fieldPath)
    {
        var normalizedFieldPath = Normalize(fieldPath);

        var property = typeof(RuleContext)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(prop => Normalize(prop.Name) == normalizedFieldPath);

        if (property != null)
        {
            return property.GetValue(context);
        }

        if (context.Metadata.Count > 0)
        {
            var metadataMatch = context.Metadata.FirstOrDefault(item => Normalize(item.Key) == normalizedFieldPath);
            if (!string.IsNullOrWhiteSpace(metadataMatch.Key))
            {
                return metadataMatch.Value;
            }
        }

        var shortFieldPath = fieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        if (!string.IsNullOrWhiteSpace(shortFieldPath) && !string.Equals(shortFieldPath, fieldPath, StringComparison.OrdinalIgnoreCase))
        {
            property = typeof(RuleContext)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(prop => Normalize(prop.Name) == Normalize(shortFieldPath));

            if (property != null)
            {
                return property.GetValue(context);
            }
        }

        return null;
    }

    private static IReadOnlyList<WorkflowNodeDefinition> ResolveNextNodes(
        string sourceNodeId,
        IReadOnlyDictionary<string, WorkflowNodeDefinition> nodes,
        IReadOnlyDictionary<string, List<WorkflowEdgeDefinition>> outgoingEdges,
        string? sourceHandle)
    {
        if (!outgoingEdges.TryGetValue(sourceNodeId, out var edges) || edges.Count == 0)
        {
            return Array.Empty<WorkflowNodeDefinition>();
        }

        IEnumerable<WorkflowEdgeDefinition> selectedEdges = edges;

        if (!string.IsNullOrWhiteSpace(sourceHandle))
        {
            var filtered = edges.Where(edge => string.Equals(edge.SourceHandle, sourceHandle, StringComparison.OrdinalIgnoreCase)).ToList();
            if (filtered.Count > 0)
            {
                selectedEdges = filtered;
            }
            else
            {
                var fallback = edges.Where(edge => string.IsNullOrWhiteSpace(edge.SourceHandle)).ToList();
                if (fallback.Count > 0)
                {
                    selectedEdges = fallback;
                }
            }
        }

        var resolvedNodes = new List<WorkflowNodeDefinition>();
        foreach (var edge in selectedEdges)
        {
            if (nodes.TryGetValue(edge.Target, out var targetNode) &&
                resolvedNodes.All(existing => !string.Equals(existing.Id, targetNode.Id, StringComparison.OrdinalIgnoreCase)))
            {
                resolvedNodes.Add(targetNode);
            }
        }

        return resolvedNodes;
    }

    private static string? GetNodeString(WorkflowNodeDefinition node, string key)
    {
        if (node.Data is null || !node.Data.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };
    }

    private static Dictionary<string, string> ExtractParameters(WorkflowNodeDefinition node)
    {
        if (node.Data is null || !node.Data.TryGetValue("params", out var paramsElement) || paramsElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in paramsElement.EnumerateObject())
        {
            parameters[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                JsonValueKind.Null => string.Empty,
                _ => property.Value.GetRawText()
            };
        }

        return parameters;
    }

    private static bool IsTriggerNode(string? nodeType)
    {
        return string.Equals(Normalize(nodeType), "triggernode", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    public sealed class WorkflowGraphDefinition
    {
        public List<WorkflowNodeDefinition> Nodes { get; set; } = [];

        public List<WorkflowEdgeDefinition> Edges { get; set; } = [];
    }

    public sealed class WorkflowNodeDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public Dictionary<string, JsonElement> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class WorkflowEdgeDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public string? SourceHandle { get; set; }

        public string? TargetHandle { get; set; }
    }
}