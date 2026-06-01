using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>change_problem_status — Problem'in boolean durum flaglerini ayarlar.</summary>
public class ChangeProblemStatusActionHandler : IWorkflowActionHandler
{
    private readonly IProblemService _problemService;

    public string ActionCode => "change_problem_status";

    public ChangeProblemStatusActionHandler(IProblemService problemService)
    {
        _problemService = problemService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "status", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = WorkflowParameterResolver.ResolveProblemId(parameters, context);
        if (problemId is null or <= 0)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "change_problem_status: problem ID çözülemedi."));

        var status = parameters.GetValueOrDefault("status", "resolved");
        var value  = parameters.GetValueOrDefault("value", "true") != "false";

        var result = _problemService.SetStatus(problemId.Value, status, value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { problemId, status, value },
                $"Problem {problemId} durumu güncellendi: {status}={value}.")
            : new ErrorDataResult<object?>(null,
                $"change_problem_status başarısız: {result.Message}"));
    }
}
