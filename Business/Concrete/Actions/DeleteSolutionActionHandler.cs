using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>delete_solution — Çözüm silme action'ı.</summary>
public class DeleteSolutionActionHandler : IWorkflowActionHandler
{
    private readonly ISolutionService     _solutionService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "delete_solution";

    public DeleteSolutionActionHandler(ISolutionService solutionService, INotificationService notificationService)
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
                new ErrorDataResult<object?>(null, "delete_solution: çözüm ID çözülemedi."));

        var reason = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("reason"), context);
        var notify = parameters.GetValueOrDefault("notifyAuthor") != "false";

        int? authorId = null;
        if (notify)
        {
            var s = _solutionService.GetById(solutionId.Value);
            if (s.Success && s.Data != null) authorId = s.Data.SenderId;
        }

        var result = _solutionService.Delete(solutionId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Çözüm silinemedi: {result.Message}"));

        if (notify && authorId.HasValue)
        {
            _notificationService.Add(new Notification
            {
                UserId    = authorId.Value,
                Title     = "Çözümünüz Silindi",
                Message   = string.IsNullOrWhiteSpace(reason)
                    ? "Paylaştığınız bir çözüm kaldırıldı."
                    : $"Paylaştığınız çözüm kaldırıldı. Sebep: {reason}",
                Type      = "warning",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { solutionId, reason }, "Çözüm silindi."));
    }
}
