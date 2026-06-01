using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>report_problem — Problem raporlama action'ı.</summary>
public class ReportProblemActionHandler : IWorkflowActionHandler
{
    private readonly IProblemService _problemService;

    public string ActionCode => "report_problem";

    public ReportProblemActionHandler(IProblemService problemService)
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
                new ErrorDataResult<object?>(null, "report_problem: problem ID çözülemedi."));

        var result = _problemService.ReportProblem(problemId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { problemId }, "Problem raporlandı.")
            : new ErrorDataResult<object?>(null, $"Raporlama başarısız: {result.Message}"));
    }
}
