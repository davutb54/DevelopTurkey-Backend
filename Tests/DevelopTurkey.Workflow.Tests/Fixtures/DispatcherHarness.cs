using Business.Abstract;
using Business.Concrete;
using Business.Concrete.Actions;
using Business.Models;
using Core.Utilities.Authorization;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DevelopTurkey.Workflow.Tests.Fixtures;

/// <summary>
/// WorkflowActionDispatcher için test harness'i.
/// Her action handler kendi mock bağımlılıklarıyla oluşturulur;
/// Dispatcher bunları IEnumerable&lt;IWorkflowActionHandler&gt; olarak alır.
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
    /// <summary>
    /// Dispatcher'ın action-level audit log'ları için ayrı mock.
    /// LogServiceMock'tan ayrı tutulur ki handler-davranış testleri (log_event vb.)
    /// dispatcher'ın audit yazımıyla karışmasın.
    /// </summary>
    public Mock<ILogService>            AuditLogServiceMock       { get; } = new(MockBehavior.Loose);
    public Mock<IWebhookClient>         WebhookClientMock         { get; } = new(MockBehavior.Loose);
    public Mock<ICapabilityResolver>    CapabilityResolverMock    { get; } = new(MockBehavior.Loose);

    public WorkflowActionDispatcher Dispatcher { get; }

    public DispatcherHarness()
    {
        // Default davranışlar — başarılı dönüşler
        EmailHelperMock.Setup(e => e.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(new SuccessResult("sent"));
        WebhookClientMock.Setup(w => w.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new WebhookSendResult(true, 200, null));

        // Test senaryolarında capability kontrolü bypass — action davranışı test edilir
        CapabilityResolverMock.Setup(r => r.Allows(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CapabilityRequestContext>()))
            .Returns(true);

        // Tüm handler'lar kendi mock bağımlılıklarıyla oluşturuluyor
        var handlers = new IWorkflowActionHandler[]
        {
            new SendEmailActionHandler(
                EmailHelperMock.Object,
                EmailTemplateServiceMock.Object,
                UserServiceMock.Object),
            new SendNotificationActionHandler(
                NotificationServiceMock.Object),
            new SendBulkNotificationActionHandler(
                UserServiceMock.Object,
                NotificationServiceMock.Object),
            new BanUserActionHandler(
                UserServiceMock.Object,
                NotificationServiceMock.Object),
            new UnbanUserActionHandler(
                UserServiceMock.Object,
                NotificationServiceMock.Object),
            new WarnUserActionHandler(
                UserWarningServiceMock.Object,
                NotificationServiceMock.Object),
            new ResolveProblemActionHandler(
                ProblemServiceMock.Object,
                NotificationServiceMock.Object),
            new HighlightProblemActionHandler(
                ProblemServiceMock.Object),
            new DeleteProblemActionHandler(
                ProblemServiceMock.Object,
                NotificationServiceMock.Object),
            new ReportProblemActionHandler(
                ProblemServiceMock.Object),
            new ApproveSolutionActionHandler(
                SolutionServiceMock.Object,
                NotificationServiceMock.Object),
            new RejectSolutionActionHandler(
                SolutionServiceMock.Object,
                NotificationServiceMock.Object),
            new HighlightSolutionActionHandler(
                SolutionServiceMock.Object),
            new DeleteSolutionActionHandler(
                SolutionServiceMock.Object,
                NotificationServiceMock.Object),
            new DeleteCommentActionHandler(
                CommentServiceMock.Object),
            new LogEventActionHandler(
                LogServiceMock.Object),
            new WebhookActionHandler(
                WebhookClientMock.Object),
        };

        Dispatcher = new WorkflowActionDispatcher(
            handlers,
            CapabilityResolverMock.Object,
            AuditLogServiceMock.Object,
            NullLogger<WorkflowActionDispatcher>.Instance);
    }

    public Dictionary<string, string> Params(params (string k, string v)[] pairs)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs) dict[k] = v;
        return dict;
    }
}
