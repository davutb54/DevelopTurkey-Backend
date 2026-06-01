using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>resolve_problem — Problemi çözüldü olarak işaretleme action'ı.</summary>
public class ResolveProblemActionHandler : IWorkflowActionHandler
{
    private readonly IProblemService      _problemService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "resolve_problem";

    public ResolveProblemActionHandler(IProblemService problemService, INotificationService notificationService)
    {
        _problemService      = problemService;
        _notificationService = notificationService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "problemTarget", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = WorkflowParameterResolver.ResolveProblemId(parameters, context);
        if (problemId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "resolve_problem: problem ID çözülemedi."));

        var result = _problemService.ResolveProblem(problemId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Problem çözülemedi: {result.Message}"));

        var notify = parameters.GetValueOrDefault("notifyOwner") != "false";
        if (notify)
        {
            var problem = _problemService.GetById(problemId.Value);
            if (problem.Success && problem.Data != null)
            {
                _notificationService.Add(new Notification
                {
                    UserId    = problem.Data.SenderId,
                    Title     = "Probleminiz Çözüldü",
                    Message   = $"'{problem.Data.Title}' başlıklı probleminiz çözüldü olarak işaretlendi.",
                    Type      = "success",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { problemId }, $"Problem çözüldü (ID: {problemId})."));
    }
}
