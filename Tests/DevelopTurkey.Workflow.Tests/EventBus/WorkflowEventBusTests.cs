using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DevelopTurkey.Workflow.Tests.EventBus;

/// <summary>
/// WorkflowEventBus in-process iteration ve error isolation testleri.
/// </summary>
public sealed class WorkflowEventBusTests
{
    [Fact]
    public async Task PublishAsync_Calls_All_Registered_Handlers()
    {
        var h1 = new Mock<IWorkflowEventHandler>();
        var h2 = new Mock<IWorkflowEventHandler>();
        var h3 = new Mock<IWorkflowEventHandler>();
        h1.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>())).Returns(Task.CompletedTask);
        h2.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>())).Returns(Task.CompletedTask);
        h3.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>())).Returns(Task.CompletedTask);

        var bus = new WorkflowEventBus(new[] { h1.Object, h2.Object, h3.Object }, NullLogger<WorkflowEventBus>.Instance);
        var ctx = new RuleContext();

        await bus.PublishAsync("test.event", ctx);

        h1.Verify(h => h.HandleAsync("test.event", ctx), Times.Once);
        h2.Verify(h => h.HandleAsync("test.event", ctx), Times.Once);
        h3.Verify(h => h.HandleAsync("test.event", ctx), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_Sets_TriggerEventName_On_Context()
    {
        var handler = new Mock<IWorkflowEventHandler>();
        RuleContext? received = null;
        handler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>()))
               .Callback<string, RuleContext>((_, c) => received = c)
               .Returns(Task.CompletedTask);

        var bus = new WorkflowEventBus(new[] { handler.Object }, NullLogger<WorkflowEventBus>.Instance);
        await bus.PublishAsync("auth.login_success", new RuleContext());

        received.Should().NotBeNull();
        received!.TriggerEventName.Should().Be("auth.login_success");
    }

    [Fact]
    public async Task PublishAsync_Empty_EventName_Skips_Everything()
    {
        var handler = new Mock<IWorkflowEventHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>())).Returns(Task.CompletedTask);
        var bus = new WorkflowEventBus(new[] { handler.Object }, NullLogger<WorkflowEventBus>.Instance);

        await bus.PublishAsync("", new RuleContext());
        await bus.PublishAsync("   ", new RuleContext());
        await bus.PublishAsync(null!, new RuleContext());

        handler.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_Handler_Exception_Does_Not_Stop_Others()
    {
        var goodCalled = false;
        var badHandler = new Mock<IWorkflowEventHandler>();
        badHandler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>()))
                  .ThrowsAsync(new InvalidOperationException("kasıtlı hata"));
        var goodHandler = new Mock<IWorkflowEventHandler>();
        goodHandler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>()))
                   .Callback(() => goodCalled = true)
                   .Returns(Task.CompletedTask);

        var bus = new WorkflowEventBus(new[] { badHandler.Object, goodHandler.Object }, NullLogger<WorkflowEventBus>.Instance);

        var act = async () => await bus.PublishAsync("e", new RuleContext());
        await act.Should().NotThrowAsync();
        goodCalled.Should().BeTrue("error isolation: bad handler diğer handler'ı bloke etmemeli");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Enricher entegrasyonu (B5 + B6 fix kapsamı)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_Calls_Enricher_Before_Handler()
    {
        var enricher = new Mock<IRuleContextEnricher>();
        enricher.Setup(e => e.EnrichAsync(It.IsAny<RuleContext>()))
                .Returns<RuleContext>(c => { c.UserRole = "Admin"; c.InstitutionId = 1; return Task.FromResult(c); });

        RuleContext? handlerSaw = null;
        var handler = new Mock<IWorkflowEventHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>()))
               .Callback<string, RuleContext>((_, c) => handlerSaw = c)
               .Returns(Task.CompletedTask);

        var bus = new WorkflowEventBus(new[] { handler.Object }, NullLogger<WorkflowEventBus>.Instance, enricher.Object);

        await bus.PublishAsync("auth.login_success", new RuleContext { SystemUserId = 9012 });

        enricher.Verify(e => e.EnrichAsync(It.IsAny<RuleContext>()), Times.Once);
        handlerSaw.Should().NotBeNull();
        handlerSaw!.UserRole.Should().Be("Admin");
        handlerSaw.InstitutionId.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_Enricher_Exception_Does_Not_Block_Handlers()
    {
        var enricher = new Mock<IRuleContextEnricher>();
        enricher.Setup(e => e.EnrichAsync(It.IsAny<RuleContext>())).ThrowsAsync(new InvalidOperationException("enricher hata"));
        var handler = new Mock<IWorkflowEventHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>())).Returns(Task.CompletedTask);

        var bus = new WorkflowEventBus(new[] { handler.Object }, NullLogger<WorkflowEventBus>.Instance, enricher.Object);

        var act = async () => await bus.PublishAsync("e", new RuleContext());
        await act.Should().NotThrowAsync();
        handler.Verify(h => h.HandleAsync("e", It.IsAny<RuleContext>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_Without_Enricher_Works_Backwards_Compatibly()
    {
        // Constructor optional parameter null → mevcut testler hala geçer.
        var handler = new Mock<IWorkflowEventHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<RuleContext>())).Returns(Task.CompletedTask);
        var bus = new WorkflowEventBus(new[] { handler.Object }, NullLogger<WorkflowEventBus>.Instance);

        await bus.PublishAsync("x", new RuleContext());
        handler.Verify(h => h.HandleAsync("x", It.IsAny<RuleContext>()), Times.Once);
    }
}
