using System.Text.Json;

namespace DevelopTurkey.Workflow.Tests.Fixtures;

/// <summary>
/// FlowJson (ReactFlow nodes + edges) inşa etmek için akıcı yardımcı.
/// </summary>
public sealed class FlowJsonBuilder
{
    private readonly List<object> _nodes = new();
    private readonly List<object> _edges = new();
    private int _nodeCounter = 0;
    private int _edgeCounter = 0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string LastNodeId { get; private set; } = string.Empty;

    public FlowJsonBuilder Trigger(string triggerCode = "auth.login_success", string? id = null)
    {
        id ??= NextId("trigger");
        _nodes.Add(new
        {
            id,
            type = "triggerNode",
            position = new { x = 0, y = 0 },
            data = new { label = "Trigger", trigger = triggerCode }
        });
        LastNodeId = id;
        return this;
    }

    public FlowJsonBuilder Condition(string field, string op, string value, string? id = null)
    {
        var previousId = LastNodeId;
        id ??= NextId("cond");
        _nodes.Add(new
        {
            id,
            type = "conditionNode",
            position = new { x = 0, y = 100 },
            data = new
            {
                label = "Condition",
                field,
                @operator = op,
                value
            }
        });
        if (!string.IsNullOrEmpty(previousId))
        {
            _edges.Add(new { id = NextEdgeId(), source = previousId, target = id });
        }
        LastNodeId = id;
        return this;
    }

    public FlowJsonBuilder Action(
        string actionCode,
        Dictionary<string, object?>? parameters = null,
        string? id = null,
        string? sourceHandle = null,
        string? fromNodeId = null)
    {
        var previousId = fromNodeId ?? LastNodeId;
        id ??= NextId("act");
        parameters ??= new Dictionary<string, object?>();
        _nodes.Add(new
        {
            id,
            type = "actionNode",
            position = new { x = 0, y = 200 },
            data = new
            {
                label = "Action",
                action = actionCode,
                @params = parameters
            }
        });
        if (!string.IsNullOrEmpty(previousId))
        {
            object edgeObj = sourceHandle is null
                ? new { id = NextEdgeId(), source = previousId, target = id }
                : new { id = NextEdgeId(), source = previousId, target = id, sourceHandle };
            _edges.Add(edgeObj);
        }
        LastNodeId = id;
        return this;
    }

    public FlowJsonBuilder CSharp(string code, string? id = null)
    {
        var previousId = LastNodeId;
        id ??= NextId("cs");
        _nodes.Add(new
        {
            id,
            type = "csharpNode",
            position = new { x = 0, y = 200 },
            data = new { label = "CS", code }
        });
        if (!string.IsNullOrEmpty(previousId))
        {
            _edges.Add(new { id = NextEdgeId(), source = previousId, target = id });
        }
        LastNodeId = id;
        return this;
    }

    public string Build()
    {
        var graph = new { nodes = _nodes, edges = _edges };
        return JsonSerializer.Serialize(graph, JsonOptions);
    }

    private string NextId(string prefix) => $"{prefix}-{++_nodeCounter}";
    private string NextEdgeId() => $"edge-{++_edgeCounter}";

    public static FlowJsonBuilder Start() => new();
}
