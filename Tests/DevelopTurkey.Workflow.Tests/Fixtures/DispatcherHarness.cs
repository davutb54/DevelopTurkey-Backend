using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DevelopTurkey.Workflow.Tests.Fixtures;

/// <summary>
/// WorkflowActionDispatcher için test harness'i — 10 bağımlılığın hepsi mock.
/// </summary>
public sealed class DispatcherHarness
{
    public Mock<IEmailHelper>           EmailHelperMock           { get; } = new(MockBehavior.Loose);
    public Mock<IEmailTemplateService>  EmailTemplateServiceMock  { get; } = new(MockBehavior.Loose);
    public Mock<INotificationService>   NotificationServiceMock   { get; } = new(MockBehavior.Loose);
    public Mock<IUserService>           UserServiceMock           { get; } = new(MockBehavior.Loose);
    public Mock<IUserWarningService>    UserWarningServiceMock    { get; } = new(MockBehavior.Loose);
    public Mock<IProblemService>        ProblemServiceMock        { get; } = new(MockBehavior.Loose);
    public Mock<ISolutionService>       SolutionServiceMock       { get; } = new(MockBehavior.Loose);
    public Mock<ICommentService>        CommentServiceMock        { get; } = new(MockBehavior.Loose);
    public Mock<ILogService>            LogServiceMock            { get; } = new(MockBehavior.Loose);
    public Mock<IWebhookClient>         WebhookClientMock         { get; } = new(MockBehavior.Loose);

    public WorkflowActionDispatcher Dispatcher { get; }

    public DispatcherHarness()
    {
        // Default davranışlar — başarılı dönüşler
        EmailHelperMock.Setup(e => e.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(new SuccessResult("sent"));
        WebhookClientMock.Setup(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                         .ReturnsAsync(new WebhookSendResult(true, 200, null));

        Dispatcher = new WorkflowActionDispatcher(
            EmailHelperMock.Object,
            EmailTemplateServiceMock.Object,
            NotificationServiceMock.Object,
            UserServiceMock.Object,
            UserWarningServiceMock.Object,
            ProblemServiceMock.Object,
            SolutionServiceMock.Object,
            CommentServiceMock.Object,
            LogServiceMock.Object,
            WebhookClientMock.Object,
            NullLogger<WorkflowActionDispatcher>.Instance);
    }

    public Dictionary<string, string> Params(params (string k, string v)[] pairs)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs) dict[k] = v;
        return dict;
    }
}
