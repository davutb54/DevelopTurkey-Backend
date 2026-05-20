using Business.Models;
using DevelopTurkey.Workflow.Tests.Fixtures;
using Moq;

namespace DevelopTurkey.Workflow.Tests.Dispatcher;

/// <summary>
/// WorkflowActionDispatcher davranış testleri: switch routing, unknown code, log_event, webhook.
/// (Tüm 18 action'ı tek tek mock'lamak yerine en kritik 3-4'üne odaklanıyoruz;
///  diğerleri downstream service çağrılarına dayanıyor, smoke testlerde gerçekten denenecek.)
/// </summary>
public sealed class DispatcherCoreTests
{
    [Fact]
    public async Task Unknown_Action_Code_Returns_Error()
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext();
        var r = await h.Dispatcher.DispatchAsync("non_existent_action", h.Params(), ctx);
        r.Success.Should().BeFalse();
        r.Message.Should().Contain("Bilinmeyen aksiyon kodu");
    }

    [Theory]
    [InlineData("log_event")]
    [InlineData("LOG_EVENT")]
    [InlineData("  log_event  ")]
    public async Task Action_Code_Is_Case_And_Whitespace_Insensitive(string code)
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext { InstitutionId = 1 };
        var r = await h.Dispatcher.DispatchAsync(code,
            h.Params(("category", "Test"), ("action", "X"), ("message", "hi")),
            ctx);
        r.Success.Should().BeTrue();
        // Default severity Info → LogInfo. Message artık prefix taşımıyor (B7 fix).
        h.LogServiceMock.Verify(l => l.LogInfo("Test", "X", "hi", It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task LogEvent_Severity_Info_Dispatches_LogInfo()
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext { InstitutionId = 1 };
        var p = h.Params(("category", "WF"), ("action", "act"), ("message", "msg"), ("severity", "Info"));
        var r = await h.Dispatcher.DispatchAsync("log_event", p, ctx);
        r.Success.Should().BeTrue();
        h.LogServiceMock.Verify(l => l.LogInfo("WF", "act", "msg", It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
    }

    [Theory]
    [InlineData("Warning")]
    [InlineData("warning")]
    [InlineData("  Warning  ")]
    public async Task LogEvent_Severity_Warning_Dispatches_LogWarning(string severity)
    {
        // B7 fix doğrulama: severity case/whitespace-insensitive olarak doğru method'a gider.
        var h = new DispatcherHarness();
        var ctx = new RuleContext { InstitutionId = 1 };
        var p = h.Params(("category", "WF"), ("action", "act"), ("message", "msg"), ("severity", severity));
        var r = await h.Dispatcher.DispatchAsync("log_event", p, ctx);
        r.Success.Should().BeTrue();
        h.LogServiceMock.Verify(l => l.LogWarning("WF", "act", "msg", It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
        h.LogServiceMock.Verify(l => l.LogInfo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task LogEvent_Severity_Error_Dispatches_LogError()
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext { InstitutionId = 1 };
        var p = h.Params(("category", "WF"), ("action", "act"), ("message", "msg"), ("severity", "Error"));
        var r = await h.Dispatcher.DispatchAsync("log_event", p, ctx);
        r.Success.Should().BeTrue();
        h.LogServiceMock.Verify(l => l.LogError("WF", "act", "msg", It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task LogEvent_Severity_Critical_Dispatches_LogCritical()
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext { InstitutionId = 1 };
        var p = h.Params(("category", "WF"), ("action", "act"), ("message", "msg"), ("severity", "Critical"));
        var r = await h.Dispatcher.DispatchAsync("log_event", p, ctx);
        r.Success.Should().BeTrue();
        h.LogServiceMock.Verify(l => l.LogCritical("WF", "act", "msg", It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task LogEvent_Severity_Unknown_Falls_Back_To_LogInfo()
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext { InstitutionId = 1 };
        var p = h.Params(("category", "WF"), ("action", "act"), ("message", "msg"), ("severity", "DebugSpam"));
        var r = await h.Dispatcher.DispatchAsync("log_event", p, ctx);
        r.Success.Should().BeTrue();
        h.LogServiceMock.Verify(l => l.LogInfo("WF", "act", "msg", It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_Calls_Client_With_Params_And_Returns_Success()
    {
        var h = new DispatcherHarness();
        var ctx = new RuleContext { SystemUserId = 5, TriggerEventName = "auth.login_success" };
        var p = h.Params(
            ("url",        "https://example.test/hook"),
            ("method",     "POST"),
            ("payload",    "{\"hello\":\"world\"}"),
            ("authHeader", "Bearer xyz")
        );
        var r = await h.Dispatcher.DispatchAsync("webhook", p, ctx);
        r.Success.Should().BeTrue();
        h.WebhookClientMock.Verify(w => w.SendAsync(
            "https://example.test/hook", "POST", It.IsAny<string>(), "Bearer xyz", default),
            Times.Once);
    }

    [Fact]
    public async Task Webhook_Failure_Returns_Error()
    {
        var h = new DispatcherHarness();
        h.WebhookClientMock.Setup(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                           .ReturnsAsync(new Business.Models.WebhookSendResult(false, 500, "Server Error"));
        var ctx = new RuleContext();
        var p = h.Params(("url", "https://x.test"), ("method", "POST"), ("payload", "{}"));
        var r = await h.Dispatcher.DispatchAsync("webhook", p, ctx);
        r.Success.Should().BeFalse();
    }

    // ───────────────────────────────────────────────────────────────────────────
    // B9 — Webhook retry / backoff
    // ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task B9_Webhook_Transient_500_Retried_3_Times_Then_Fails()
    {
        var h = new DispatcherHarness();
        h.WebhookClientMock
            .Setup(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new Business.Models.WebhookSendResult(false, 503, "Service Unavailable"));

        var p = h.Params(("url", "https://x.test"), ("method", "POST"), ("payload", "{}"));
        var r = await h.Dispatcher.DispatchAsync("webhook", p, new RuleContext());

        r.Success.Should().BeFalse();
        h.WebhookClientMock.Verify(
            w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Exactly(3), "503 geçici hata → 3 deneme");
    }

    [Fact]
    public async Task B9_Webhook_Recovers_On_Third_Attempt()
    {
        var h = new DispatcherHarness();
        h.WebhookClientMock
            .SetupSequence(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new Business.Models.WebhookSendResult(false, 502, "Bad Gateway"))
            .ReturnsAsync(new Business.Models.WebhookSendResult(false, 0,   "timeout"))
            .ReturnsAsync(new Business.Models.WebhookSendResult(true,  200, null));

        var p = h.Params(("url", "https://x.test"), ("method", "POST"), ("payload", "{}"));
        var r = await h.Dispatcher.DispatchAsync("webhook", p, new RuleContext());

        r.Success.Should().BeTrue("3. denemede 200 döndü");
        h.WebhookClientMock.Verify(
            w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Exactly(3));
    }

    [Theory]
    [InlineData(400)]   // Bad Request
    [InlineData(401)]   // Unauthorized
    [InlineData(403)]   // Forbidden
    [InlineData(404)]   // Not Found
    public async Task B9_Webhook_Permanent_4xx_Not_Retried(int statusCode)
    {
        var h = new DispatcherHarness();
        h.WebhookClientMock
            .Setup(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new Business.Models.WebhookSendResult(false, statusCode, "client error"));

        var p = h.Params(("url", "https://x.test"), ("method", "POST"), ("payload", "{}"));
        var r = await h.Dispatcher.DispatchAsync("webhook", p, new RuleContext());

        r.Success.Should().BeFalse();
        h.WebhookClientMock.Verify(
            w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Once, $"{statusCode} kalıcı istemci hatası → retry yok");
    }

    [Fact]
    public async Task B9_Webhook_429_Rate_Limit_Is_Retried()
    {
        var h = new DispatcherHarness();
        h.WebhookClientMock
            .SetupSequence(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new Business.Models.WebhookSendResult(false, 429, "Too Many Requests"))
            .ReturnsAsync(new Business.Models.WebhookSendResult(true,  200, null));

        var p = h.Params(("url", "https://x.test"), ("method", "POST"), ("payload", "{}"));
        var r = await h.Dispatcher.DispatchAsync("webhook", p, new RuleContext());

        r.Success.Should().BeTrue();
        h.WebhookClientMock.Verify(
            w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Exactly(2), "429 geçici → retry edilir");
    }

    [Fact]
    public async Task B9_Webhook_First_Attempt_Success_No_Retry()
    {
        var h = new DispatcherHarness();
        h.WebhookClientMock
            .Setup(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new Business.Models.WebhookSendResult(true, 200, null));

        var p = h.Params(("url", "https://x.test"), ("method", "POST"), ("payload", "{}"));
        var r = await h.Dispatcher.DispatchAsync("webhook", p, new RuleContext());

        r.Success.Should().BeTrue();
        h.WebhookClientMock.Verify(
            w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Once);
    }

    [Fact]
    public async Task Exception_In_Handler_Is_Caught_And_Returned_As_Error()
    {
        var h = new DispatcherHarness();
        h.LogServiceMock.Setup(l => l.LogInfo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                        .Throws(new InvalidOperationException("DB down"));
        var ctx = new RuleContext { InstitutionId = 1 };
        var p = h.Params(("category", "X"), ("action", "Y"), ("message", "Z"));
        var r = await h.Dispatcher.DispatchAsync("log_event", p, ctx);
        r.Success.Should().BeFalse();
        r.Message.Should().Contain("Aksiyon hatası");
        r.Message.Should().Contain("DB down");
    }
}
