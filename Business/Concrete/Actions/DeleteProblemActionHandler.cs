using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>delete_problem — Problem silme action'ı.</summary>
public class DeleteProblemActionHandler : IWorkflowActionHandler
{
    private readonly IProblemService      _problemService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "delete_problem";

    public DeleteProblemActionHandler(IProblemService problemService, INotificationService notificationService)
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
                new ErrorDataResult<object?>(null, "delete_problem: problem ID çözülemedi."));

        var reason = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("reason"), context);
        var notify = parameters.GetValueOrDefault("notifyOwner") != "false";

        int? ownerId = null;
        string? problemTitle = null;
        if (notify)
        {
            var problem = _problemService.GetById(problemId.Value);
            if (problem.Success && problem.Data != null)
            {
                ownerId      = problem.Data.SenderId;
                problemTitle = problem.Data.Title;
            }
        }

        var result = _problemService.Delete(problemId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Problem silinemedi: {result.Message}"));

        if (notify && ownerId.HasValue)
        {
            _notificationService.Add(new Notification
            {
                UserId    = ownerId.Value,
                Title     = "Probleminiz Silindi",
                Message   = string.IsNullOrWhiteSpace(reason)
                    ? $"'{problemTitle}' başlıklı probleminiz kaldırıldı."
                    : $"'{problemTitle}' başlıklı probleminiz kaldırıldı. Sebep: {reason}",
                Type      = "warning",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { problemId, reason }, "Problem silindi."));
    }
}
