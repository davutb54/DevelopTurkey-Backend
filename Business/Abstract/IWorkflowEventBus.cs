using Business.Models;

namespace Business.Abstract;

public interface IWorkflowEventBus
{
    /// <summary>
    /// Belirtilen event'i yayınlar; kayıtlı tüm handler'lar sırayla çağrılır.
    /// </summary>
    Task PublishAsync(string eventName, RuleContext context);
}
