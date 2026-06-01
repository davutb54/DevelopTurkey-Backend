using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>reject_solution — Çözüm reddetme action'ı.</summary>
public class RejectSolutionActionHandler : IWorkflowActionHandler
{
    private readonly ISolutionService     _solutionService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "reject_solution";

    public RejectSolutionActionHandler(ISolutionService solutionService, INotificationService notificationService)
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
                new ErrorDataResult<object?>(null, "reject_solution: çözüm ID çözülemedi."));

        var reason = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("reason"), context);
        var result = _solutionService.RejectSolution(solutionId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Çözüm reddedilemedi: {result.Message}"));

        var notify = parameters.GetValueOrDefault("notifyAuthor") != "false";
        if (notify)
        {
            var solution = _solutionService.GetById(solutionId.Value);
            if (solution.Success && solution.Data != null)
            {
                _notificationService.Add(new Notification
                {
                    UserId    = solution.Data.SenderId,
                    Title     = "Çözümünüz Reddedildi",
                    Message   = string.IsNullOrWhiteSpace(reason)
                        ? "Paylaştığınız çözüm uzman değerlendirmesinden geçemedi."
                        : $"Paylaştığınız çözüm reddedildi. Sebep: {reason}",
                    Type      = "warning",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { solutionId, reason }, "Çözüm reddedildi."));
    }
}
