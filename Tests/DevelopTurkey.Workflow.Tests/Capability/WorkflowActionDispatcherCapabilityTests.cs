using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Core.Utilities.Authorization;
using Core.Utilities.Helpers.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DevelopTurkey.Workflow.Tests.Capability;

/// <summary>
/// WorkflowActionDispatcher capability gating (2 senaryo):
///   1. SystemUser ilgili workflow.action.X capability'sine sahip → action çalışır
///   2. SystemUser capability'ye sahip değil → ErrorDataResult döner, action çalışmaz
/// </summary>
public sealed class WorkflowActionDispatcherCapabilityTests
{
    private const int SystemUserId = 2;

    // ─── Yardımcı fabrika ─────────────────────────────────────────────────

    private static (WorkflowActionDispatcher dispatcher, Mock<ICapabilityResolver> resolverMock)
        BuildDispatcher(bool capabilityAllowed)
    {
        var resolver = new Mock<ICapabilityResolver>();
        resolver
            .Setup(r => r.Allows(SystemUserId, It.IsAny<string>(), null))
            .Returns(capabilityAllowed);

        var dispatcher = new WorkflowActionDispatcher(
            emailHelper:            new Mock<IEmailHelper>().Object,
            emailTemplateService:   new Mock<IEmailTemplateService>().Object,
            notificationService:    new Mock<INotificationService>().Object,
            userService:            new Mock<IUserService>().Object,
            userWarningService:     new Mock<IUserWarningService>().Object,
            problemService:         new Mock<IProblemService>().Object,
            solutionService:        new Mock<ISolutionService>().Object,
            commentService:         new Mock<ICommentService>().Object,
            logService:             new Mock<ILogService>().Object,
            webhookClient:          new Mock<IWebhookClient>().Object,
            capabilityResolver:     resolver.Object,
            logger:                 NullLogger<WorkflowActionDispatcher>.Instance);

        return (dispatcher, resolver);
    }

    private static RuleContext MakeContext() => new()
    {
        SystemUserId    = SystemUserId,
        TriggerEventName = "problem.created",
        InstitutionId   = 1,
    };

    // ─── Senaryo 1: Capability var → action çalıştırılmaya çalışılır ─────
    // Not: "log_event" action'ı bağımlılık gerektirmez; dispatcher içinde
    //       _logService.Add() çağrısı yapılır — servis mock'lu, exception yok.

    [Fact]
    public async Task DispatchAsync_WithCapability_DoesNotReturnCapabilityDenied()
    {
        var (dispatcher, _) = BuildDispatcher(capabilityAllowed: true);

        // log_event: dış bağımlılığı en az action
        var result = await dispatcher.DispatchAsync(
            "log_event",
            new Dictionary<string, string> { ["level"] = "info", ["message"] = "test" },
            MakeContext());

        // Capability deny mesajı içermemeli
        result.Message.Should().NotContain("Yetkisiz action");
        result.Message.Should().NotContain("capability_denied");
    }

    // ─── Senaryo 2: Capability yok → ErrorDataResult, action hiç çalışmaz ─

    [Fact]
    public async Task DispatchAsync_WithoutCapability_ReturnsCapabilityDeniedError()
    {
        var (dispatcher, resolverMock) = BuildDispatcher(capabilityAllowed: false);

        var result = await dispatcher.DispatchAsync(
            "ban_user",
            new Dictionary<string, string> { ["userId"] = "5" },
            MakeContext());

        // Capability denied mesajı olmalı
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Yetkisiz action");
        result.Message.Should().Contain("workflow.action.ban_user");

        // Resolver Allows çağrıldı mı doğrula
        resolverMock.Verify(
            r => r.Allows(SystemUserId, "workflow.action.ban_user", null),
            Times.Once);
    }
}
