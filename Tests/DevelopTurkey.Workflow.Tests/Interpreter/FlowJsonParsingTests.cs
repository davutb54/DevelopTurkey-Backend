using Business.Models;
using DevelopTurkey.Workflow.Tests.Fixtures;

namespace DevelopTurkey.Workflow.Tests.Interpreter;

/// <summary>
/// FlowJson parsing edge case'leri.
/// </summary>
public sealed class FlowJsonParsingTests
{
    [Fact]
    public async Task Empty_FlowJson_Returns_Error()
    {
        var harness = new InterpreterHarness();
        var result = await harness.Manager.ExecuteWorkflowAsync("", new RuleContext());
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("FlowJson");
    }

    [Fact]
    public async Task Null_Context_Returns_Error()
    {
        var harness = new InterpreterHarness();
        var result = await harness.Manager.ExecuteWorkflowAsync("{\"nodes\":[],\"edges\":[]}", null!);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("RuleContext");
    }

    [Fact]
    public async Task Invalid_Json_Returns_JsonError()
    {
        var harness = new InterpreterHarness();
        var result = await harness.Manager.ExecuteWorkflowAsync("{not-valid-json", new RuleContext());
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Workflow JSON ayrıştırma hatası");
    }

    [Fact]
    public async Task No_Nodes_Returns_Error()
    {
        var harness = new InterpreterHarness();
        var result = await harness.Manager.ExecuteWorkflowAsync("{\"nodes\":[],\"edges\":[]}", new RuleContext());
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Workflow içinde çalıştırılacak node bulunamadı.");
    }

    [Fact]
    public async Task Trigger_Only_Works_But_No_Dispatch()
    {
        var harness = new InterpreterHarness();
        var flow = FlowJsonBuilder.Start().Trigger().Build();
        var result = await harness.RunAsync(flow, new RuleContext());
        result.Success.Should().BeTrue();
        result.Dispatched.Should().BeEmpty();
    }
}
