using Business.Abstract;
using Business.Models;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// In-process event bus: kayıtlı tüm IWorkflowEventHandler'ları çağırır.
/// Yeni handler eklemek için DI'a IWorkflowEventHandler olarak kaydetmek yeterlidir.
///
/// Publish anında IRuleContextEnricher (varsa) çağırılarak context merkezi olarak
/// zenginleştirilir (B5 + B6 fix): SystemUserId → UserRole + UserSnapshot +
/// InstitutionId; ProblemId → ProblemStatus + ProblemSnapshot, vb.
/// </summary>
public sealed class WorkflowEventBus : IWorkflowEventBus
{
    private readonly IEnumerable<IWorkflowEventHandler> _handlers;
    private readonly IRuleContextEnricher? _enricher;
    private readonly ILogger<WorkflowEventBus> _logger;

    public WorkflowEventBus(
        IEnumerable<IWorkflowEventHandler> handlers,
        ILogger<WorkflowEventBus> logger,
        IRuleContextEnricher? enricher = null)
    {
        _handlers = handlers;
        _logger = logger;
        _enricher = enricher;
    }

    public async Task PublishAsync(string eventName, RuleContext context)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return;

        // context.TriggerEventName'i bus katmanında doldur; handler'lar override etmez
        context.TriggerEventName = eventName;

        // Context'i merkezi olarak zenginleştir (B5+B6): publish noktası eksik bilgi
        // gönderse bile burada UserRole/InstitutionId/UserSnapshot/ProblemSnapshot dolar.
        if (_enricher != null)
        {
            try
            {
                context = await _enricher.EnrichAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[WorkflowEventBus] Context enrichment hatası — partial context kullanılacak. Event={Event}",
                    eventName);
            }
        }

        _logger.LogInformation(
            "[WorkflowEventBus] Event yayınlanıyor. Event={Event}, Institution={Institution}, UserId={UserId}, Role={Role}",
            eventName, context.InstitutionId, context.SystemUserId, context.UserRole);

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.HandleAsync(eventName, context);
            }
            catch (Exception ex)
            {
                // Bir handler patlasa bile diğerleri çalışmaya devam eder
                _logger.LogError(ex,
                    "[WorkflowEventBus] Handler {Handler} Event={Event} için hata fırlattı.",
                    handler.GetType().Name, eventName);
            }
        }
    }
}
