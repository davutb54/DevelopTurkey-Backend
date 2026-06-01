using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>send_email — E-posta gönderme action'ı.</summary>
public class SendEmailActionHandler : IWorkflowActionHandler
{
    private readonly IEmailHelper _emailHelper;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUserService _userService;

    public string ActionCode => "send_email";

    public SendEmailActionHandler(
        IEmailHelper emailHelper,
        IEmailTemplateService emailTemplateService,
        IUserService userService)
    {
        _emailHelper          = emailHelper;
        _emailTemplateService = emailTemplateService;
        _userService          = userService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        var recipient = parameters.GetValueOrDefault("recipient");
        if (string.IsNullOrWhiteSpace(recipient))
            errors.Add("send_email: 'recipient' parametresi zorunludur.");
        if (recipient == "custom" && string.IsNullOrWhiteSpace(parameters.GetValueOrDefault("customTo")))
            errors.Add("send_email: recipient=custom ise 'customTo' gereklidir.");
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var to = ResolveEmailAddress(parameters, context);
        if (string.IsNullOrWhiteSpace(to))
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "send_email: alıcı e-posta adresi çözülemedi."));

        var (subject, body) = ResolveEmailContent(parameters, context);
        var result = _emailHelper.Send(to, subject, body);

        var cc = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("cc"), context);
        if (result.Success && !string.IsNullOrWhiteSpace(cc))
        {
            foreach (var addr in cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _emailHelper.Send(addr, subject, body);
        }

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { to, subject }, "E-posta gönderildi.")
            : new ErrorDataResult<object?>(null, $"E-posta gönderilemedi: {result.Message}"));
    }

    // ── Yardımcılar ────────────────────────────────────────────────────────────

    private string? ResolveEmailAddress(Dictionary<string, string> parameters, RuleContext context)
    {
        var recipientType = parameters.GetValueOrDefault("recipient") ?? "context_user";
        return recipientType switch
        {
            "target_user" when context.TargetUserId.HasValue
                => _userService.GetById(context.TargetUserId.Value).Data?.Email,
            "target_user"
                => context.UserSnapshot?.Email,
            "custom"
                => WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("customTo"), context),
            _ => context.UserSnapshot?.Email
        };
    }

    private (string subject, string body) ResolveEmailContent(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var templateKey = WorkflowParameterResolver.Resolve(
            parameters.GetValueOrDefault("templateKey"), context);

        if (!string.IsNullOrWhiteSpace(templateKey))
        {
            var tmplResult = _emailTemplateService.GetByKey(templateKey);
            if (tmplResult.Success && tmplResult.Data != null)
            {
                var placeholders = BuildContextPlaceholders(context);
                var subject = _emailTemplateService.RenderTemplate(tmplResult.Data.Subject, placeholders).Data
                              ?? tmplResult.Data.Subject;
                var body    = _emailTemplateService.RenderTemplate(tmplResult.Data.Body, placeholders).Data
                              ?? tmplResult.Data.Body;
                return (subject, body);
            }
        }

        return (
            WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("subject"), context),
            WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("body"),    context)
        );
    }

    private static Dictionary<string, string> BuildContextPlaceholders(RuleContext context)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (context.UserSnapshot != null)
        {
            d["Name"]     = context.UserSnapshot.Name;
            d["Surname"]  = context.UserSnapshot.Surname;
            d["Email"]    = context.UserSnapshot.Email;
            d["UserName"] = context.UserSnapshot.UserName;
        }
        if (context.ProblemSnapshot  != null) d["ProblemTitle"] = context.ProblemSnapshot.Title;
        if (context.SolutionSnapshot != null) d["SolutionId"]   = context.SolutionSnapshot.Id.ToString();
        d["TriggerEvent"]  = context.TriggerEventName;
        d["SystemUserId"]  = context.SystemUserId.ToString();
        d["InstitutionId"] = context.InstitutionId?.ToString() ?? "";
        return d;
    }
}
