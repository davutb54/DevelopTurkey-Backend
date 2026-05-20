using Business.Models;
using Core.Utilities.Results;
using DevelopTurkey.Workflow.Tests.Fixtures;
using Moq;

namespace DevelopTurkey.Workflow.Tests.Interpreter;

/// <summary>
/// WorkflowInterpreterManager DFS traversal davranışı.
/// - Trigger node başlangıç işaretidir, yan etkisi yoktur, traversal devam eder.
/// - Cycle prevention: zaten ziyaret edilen node ikinci kez gezilmez.
/// - Action node IWorkflowActionDispatcher'a devredilir.
/// - CSharp node IRuleExecutionService'a devredilir.
/// </summary>
public sealed class DfsTraversalTests
{
    [Fact]
    public async Task Trigger_Then_Action_Dispatches_Once()
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { SystemUserId = 1 };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Action("log_event", new() { ["msg"] = "hi" })
            .Build();

        var result = await harness.RunAsync(flow, ctx);

        result.Success.Should().BeTrue();
        result.NodeCount.Should().Be(2);
        result.VisitedNodeCount.Should().Be(2);
        result.Dispatched.Should().HaveCount(1);
        result.Dispatched[0].ActionCode.Should().Be("log_event");
        result.Trace.Should().Contain(t => t.Contains("Trigger node — başlangıç noktası"));  // İ1 fix
        result.Trace.Should().Contain(t => t.Contains("ActionNode devredildi: log_event"));
    }

    [Fact]
    public async Task Trigger_Condition_True_Goes_Yes_Branch_Only()
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = "Admin" };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "eq", "Admin")
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();

        var result = await harness.RunAsync(flow, ctx);

        result.Dispatched.Should().ContainSingle();
        result.Dispatched[0].Parameters["b"].Should().Be("YES");
    }

    [Fact]
    public async Task Trigger_Condition_False_Goes_No_Branch_Only()
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = "User" };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "eq", "Admin")
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();

        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched.Should().ContainSingle();
        result.Dispatched[0].Parameters["b"].Should().Be("NO");
    }

    [Fact]
    public async Task Multiple_Actions_In_Sequence_All_Dispatched()
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { SystemUserId = 1 };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Action("log_event",       new() { ["m"] = "1" })
            .Action("send_notification", new() { ["m"] = "2" })
            .Action("webhook",         new() { ["m"] = "3" })
            .Build();

        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched.Should().HaveCount(3);
        result.Dispatched.Select(d => d.ActionCode).Should().Equal("log_event", "send_notification", "webhook");
    }

    [Fact]
    public async Task CSharp_Node_Delegates_To_RuleExecutionService()
    {
        var harness = new InterpreterHarness();
        harness.RuleExecutionServiceMock
            .Setup(s => s.ExecuteCSharpNodeAsync("return 42;", It.IsAny<RuleContext>()))
            .ReturnsAsync(new SuccessDataResult<object?>(42, "ok"));

        var ctx = new RuleContext { SystemUserId = 1 };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .CSharp("return 42;")
            .Build();

        var result = await harness.RunAsync(flow, ctx);
        result.Trace.Should().Contain(t => t.Contains("CSharpNode başarıyla çalıştı."));
        harness.RuleExecutionServiceMock.Verify(s => s.ExecuteCSharpNodeAsync("return 42;", It.IsAny<RuleContext>()), Times.Once);
    }

    [Fact]
    public async Task CSharp_Node_Failure_Reported_In_Trace()
    {
        var harness = new InterpreterHarness();
        harness.RuleExecutionServiceMock
            .Setup(s => s.ExecuteCSharpNodeAsync(It.IsAny<string>(), It.IsAny<RuleContext>()))
            .ReturnsAsync(new ErrorDataResult<object?>(null, "Yetkisiz"));

        var ctx = new RuleContext { SystemUserId = 1 };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .CSharp("malicious()")
            .Build();

        var result = await harness.RunAsync(flow, ctx);
        result.Trace.Should().Contain(t => t.Contains("CSharpNode hata verdi: Yetkisiz"));
        // B4 fix: yapısal hata listesi de doluyor
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("CSharpNode") && e.Contains("Yetkisiz"));
    }

    [Fact]
    public async Task Action_Dispatcher_Failure_Reported_In_Trace_But_Continues()
    {
        var harness = new InterpreterHarness();
        // İlk action başarısız, ikincisi başarılı olmalı (loose mock default)
        harness.ActionDispatcherMock.Reset();
        harness.ActionDispatcherMock
            .Setup(d => d.DispatchAsync("bad_action", It.IsAny<Dictionary<string, string>>(), It.IsAny<RuleContext>()))
            .ReturnsAsync(new ErrorDataResult<object?>(null, "Eksik parametre"));
        harness.ActionDispatcherMock
            .Setup(d => d.DispatchAsync("log_event", It.IsAny<Dictionary<string, string>>(), It.IsAny<RuleContext>()))
            .ReturnsAsync(new SuccessDataResult<object?>(null, "ok"));

        var ctx = new RuleContext { SystemUserId = 1 };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Action("bad_action")
            .Action("log_event")
            .Build();

        var result = await harness.RunAsync(flow, ctx);
        result.Success.Should().BeTrue();  // workflow tamamlandı, ama bir action hata verdi
        result.Trace.Should().Contain(t => t.Contains("ActionNode hata verdi"));
        result.Trace.Should().Contain(t => t.Contains("ActionNode devredildi: log_event"));
        // B4 fix: hata listesinde bad_action var, log_event yok
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("bad_action") && e.Contains("Eksik parametre"));
        result.Errors.Should().NotContain(e => e.Contains("log_event"));
    }

    [Fact]
    public async Task Cycle_In_Graph_Does_Not_Loop_Infinitely()
    {
        // node-1 → node-2 → node-1 (cycle)
        // Beklenti: her node bir kez visit edilir, trace'de "zaten ziyaret edildi" geçer.
        var flow = """
        {
          "nodes": [
            { "id": "trigger-1", "type": "triggerNode", "data": { "trigger": "x" } },
            { "id": "act-1", "type": "actionNode", "data": { "action": "log_event", "params": {} } },
            { "id": "act-2", "type": "actionNode", "data": { "action": "log_event", "params": {} } }
          ],
          "edges": [
            { "id": "e1", "source": "trigger-1", "target": "act-1" },
            { "id": "e2", "source": "act-1", "target": "act-2" },
            { "id": "e3", "source": "act-2", "target": "act-1" }
          ]
        }
        """;

        var harness = new InterpreterHarness();
        var ctx = new RuleContext();
        var result = await harness.RunAsync(flow, ctx);

        result.Success.Should().BeTrue();
        result.VisitedNodeCount.Should().Be(3);
        result.Trace.Should().Contain(t => t.Contains("zaten ziyaret edildi"));
    }

    // ───────────────────────────────────────────────────────────────────────────
    // B4 fix — yapısal hata raporlama
    // ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Happy_Path_Has_Empty_Errors_List_And_HasErrors_False()
    {
        var harness = new InterpreterHarness();
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Action("log_event", new() { ["msg"] = "ok" })
            .Build();

        var result = await harness.RunAsync(flow, new RuleContext());

        result.Success.Should().BeTrue();
        result.HasErrors.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Multiple_Failing_Actions_Produce_Multiple_Errors()
    {
        var harness = new InterpreterHarness();
        harness.ActionDispatcherMock.Reset();
        harness.ActionDispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<RuleContext>()))
            .ReturnsAsync(new ErrorDataResult<object?>(null, "boom"));

        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Action("a1")
            .Action("a2")
            .Action("a3")
            .Build();

        var result = await harness.RunAsync(flow, new RuleContext());

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().HaveCount(3, "her başarısız action ayrı bir error üretir");
        result.Errors.Should().Contain(e => e.Contains("a1"));
        result.Errors.Should().Contain(e => e.Contains("a2"));
        result.Errors.Should().Contain(e => e.Contains("a3"));
    }

    [Fact]
    public async Task Error_Message_Contains_NodeId_And_ActionCode_For_Diagnostics()
    {
        var harness = new InterpreterHarness();
        harness.ActionDispatcherMock.Reset();
        harness.ActionDispatcherMock
            .Setup(d => d.DispatchAsync("send_email", It.IsAny<Dictionary<string, string>>(), It.IsAny<RuleContext>()))
            .ReturnsAsync(new ErrorDataResult<object?>(null, "SMTP timeout"));

        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Action("send_email", new() { ["to"] = "x" })
            .Build();

        var result = await harness.RunAsync(flow, new RuleContext());

        result.Errors.Should().ContainSingle().Which.Should().Match(e =>
            e.Contains("send_email") && e.Contains("SMTP timeout"));
    }
}
