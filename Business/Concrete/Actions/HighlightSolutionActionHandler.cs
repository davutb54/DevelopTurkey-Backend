using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>highlight_solution — Çözüm öne çıkarma durumu toggle action'ı.</summary>
public class HighlightSolutionActionHandler : IWorkflowActionHandler
{
    private readonly ISolutionService _solutionService;

    public string ActionCode => "highlight_solution";

    public HighlightSolutionActionHandler(ISolutionService solutionService)
    {
        _solutionService = solutionService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "solutionTarget", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var solutionId = WorkflowParameterResolver.ResolveSolutionId(parameters, context);
        if (solutionId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "highlight_solution: çözüm ID çözülemedi."));

        var result = _solutionService.ToggleHighlight(solutionId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { solutionId }, "Çözüm öne çıkarma durumu değiştirildi.")
            : new ErrorDataResult<object?>(null, $"İşlem başarısız: {result.Message}"));
    }
}
