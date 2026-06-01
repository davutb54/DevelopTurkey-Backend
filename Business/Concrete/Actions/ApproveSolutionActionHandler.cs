using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>approve_solution — Çözüm onaylama action'ı.</summary>
public class ApproveSolutionActionHandler : IWorkflowActionHandler
{
    private readonly ISolutionService     _solutionService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "approve_solution";

    public ApproveSolutionActionHandler(ISolutionService solutionService, INotificationService notificationService)
    {
        _solutionService     = solutionService;
        _notificationService = notificationService;
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
                new ErrorDataResult<object?>(null, "approve_solution: çözüm ID çözülemedi."));

        var result = _solutionService.ApproveSolution(solutionId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Çözüm onaylanamadı: {result.Message}"));

        var notify = parameters.GetValueOrDefault("notifyAuthor") != "false";
        if (notify)
        {
            var solution = _solutionService.GetById(solutionId.Value);
            if (solution.Success && solution.Data != null)
            {
                _notificationService.Add(new Notification
                {
                    UserId    = solution.Data.SenderId,
                    Title     = "Çözümünüz Onaylandı",
                    Message   = "Paylaştığınız çözüm uzman ekibimiz tarafından onaylandı.",
                    Type      = "success",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { solutionId }, "Çözüm onaylandı."));
    }
}
