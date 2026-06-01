using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>assign_problem_institution — Problemi belirli bir kuruma atar.</summary>
public class AssignProblemToInstitutionActionHandler : IWorkflowActionHandler
{
    private readonly IProblemService _problemService;

    public string ActionCode => "assign_problem_institution";

    public AssignProblemToInstitutionActionHandler(IProblemService problemService)
    {
        _problemService = problemService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "institutionId", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = WorkflowParameterResolver.ResolveProblemId(parameters, context);
        if (problemId is null or <= 0)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "assign_problem_institution: problem ID çözülemedi."));

        var institutionIdRaw = WorkflowParameterResolver.Resolve(
            parameters.GetValueOrDefault("institutionId"), context);

        if (!int.TryParse(institutionIdRaw, out var institutionId) || institutionId <= 0)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "assign_problem_institution: geçerli bir institutionId gerekli."));

        var result = _problemService.AssignToInstitution(problemId.Value, institutionId);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { problemId, institutionId },
                $"Problem {problemId} kuruma atandı (InstitutionId: {institutionId}).")
            : new ErrorDataResult<object?>(null,
                $"assign_problem_institution başarısız: {result.Message}"));
    }
}
