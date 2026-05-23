using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

/// <summary>
/// Bir tetikleyici event geldiğinde uygun workflow definition'larını bulur,
/// WorkflowRun kayıtlarını oluşturur ve root node'ları kuyruğa gönderir.
/// Sprint 1: temel akış (idempotency + NodeRun planlaması).
/// Faz 3: tam execution engine entegrasyonu.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Bir event için uygun tüm aktif workflow definition'ları bulur ve
    /// her biri için WorkflowRun + NodeRun oluşturup kuyruğa gönderir.
    /// </summary>
    /// <param name="triggerEvent">Tetiklenen event kodu (örn: "problem.created")</param>
    /// <param name="institutionId">Kuruma özgü kural filtrelemesi</param>
    /// <param name="contextJson">RuleContext JSON snapshot</param>
    /// <param name="eventId">İdempotency Guid — aynı event tekrar gelirse aynı run döner</param>
    /// <param name="triggeredByUserId">Tetikleyen kullanıcı</param>
    /// <param name="isDryRun">True ise action'lar simüle edilir</param>
    Task<IDataResult<List<WorkflowRun>>> StartAsync(
        string triggerEvent,
        int institutionId,
        string contextJson,
        Guid eventId,
        int triggeredByUserId,
        bool isDryRun = false);

    /// <summary>
    /// Tek bir workflow definition + version için WorkflowRun başlatır.
    /// Idempotency: aynı (eventId, definitionId, versionId) üçlüsü varsa mevcut run'ı döner.
    /// </summary>
    Task<IDataResult<WorkflowRun>> StartSingleAsync(
        int definitionId,
        int versionId,
        string flowJson,
        string triggerEvent,
        int institutionId,
        string contextJson,
        Guid eventId,
        int triggeredByUserId,
        bool isDryRun = false);
}
