using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Core.Utilities.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace DevelopTurkey.Workflow.Tests.Fixtures;

/// <summary>
/// WorkflowInterpreterManager için test harness'i.
/// IRuleExecutionService ve IWorkflowActionDispatcher mock'larını birlikte tutar.
/// </summary>
public sealed class InterpreterHarness
{
    public Mock<IRuleExecutionService> RuleExecutionServiceMock { get; } = new(MockBehavior.Strict);
    public Mock<IWorkflowActionDispatcher> ActionDispatcherMock { get; } = new(MockBehavior.Loose);
    public WorkflowInterpreterManager Manager { get; }

    /// <summary>
    /// Dispatcher tarafından çağrılan action'ların listesi (test assertion için).
    /// </summary>
    public List<DispatchedCall> DispatchedActions { get; } = new();

    public sealed record DispatchedCall(string ActionCode, IDictionary<string, string> Parameters);

    public InterpreterHarness()
    {
        // Default: CSharp script çağrısı uyarısız geçer (override edilebilir)
        RuleExecutionServiceMock
            .Setup(s => s.ExecuteCSharpNodeAsync(It.IsAny<string>(), It.IsAny<RuleContext>()))
            .ReturnsAsync(new SuccessDataResult<object?>(null, "CSharp ok"));

        // Default: tüm action'lar başarılı, çağrı listesine eklenir
        ActionDispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<RuleContext>()))
            .Returns((string code, Dictionary<string, string> p, RuleContext _) =>
            {
                DispatchedActions.Add(new DispatchedCall(code, p));
                return Task.FromResult<IDataResult<object?>>(new SuccessDataResult<object?>(null, "ok"));
            });

        Manager = new WorkflowInterpreterManager(
            RuleExecutionServiceMock.Object,
            ActionDispatcherMock.Object,
            NullLogger<WorkflowInterpreterManager>.Instance);
    }

    /// <summary>
    /// Run workflow ve dönen trace'i çıkar.
    /// </summary>
    public async Task<RunResult> RunAsync(string flowJson, RuleContext context)
    {
        var result = await Manager.ExecuteWorkflowAsync(flowJson, context);
        var trace = new List<string>();
        var errors = new List<string>();
        bool hasErrors = false;
        int nodeCount = 0, visitedCount = 0;
        if (result.Success && result.Data is not null)
        {
            var json = JsonSerializer.Serialize(result.Data);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("trace", out var t) && t.ValueKind == JsonValueKind.Array)
                trace = t.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            if (doc.RootElement.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array)
                errors = e.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            if (doc.RootElement.TryGetProperty("hasErrors", out var h))
                hasErrors = h.GetBoolean();
            if (doc.RootElement.TryGetProperty("nodeCount", out var n))
                nodeCount = n.GetInt32();
            if (doc.RootElement.TryGetProperty("visitedNodeCount", out var v))
                visitedCount = v.GetInt32();
        }
        return new RunResult(result.Success, result.Message, trace, errors, hasErrors, nodeCount, visitedCount, DispatchedActions);
    }

    public sealed record RunResult(
        bool Success,
        string? Message,
        IReadOnlyList<string> Trace,
        IReadOnlyList<string> Errors,
        bool HasErrors,
        int NodeCount,
        int VisitedNodeCount,
        IReadOnlyList<DispatchedCall> Dispatched);
}
