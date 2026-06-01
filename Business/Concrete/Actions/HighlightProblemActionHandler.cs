using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>highlight_problem — Problem öne çıkarma durumu toggle action'ı.</summary>
public class HighlightProblemActionHandler : IWorkflowActionHandler
{
    private readonly IProblemService _problemService;

    public string ActionCode => "highlight_problem";

    public HighlightProblemActionHandler(IProblemService problemService)
    {
        _problemService = problemService;
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
                new ErrorDataResult<object?>(null, "highlight_problem: problem ID çözülemedi."));

        var result = _problemService.ToggleHighlight(problemId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { problemId }, "Problem öne çıkarma durumu değiştirildi.")
            : new ErrorDataResult<object?>(null, $"İşlem başarısız: {result.Message}"));
    }
}
