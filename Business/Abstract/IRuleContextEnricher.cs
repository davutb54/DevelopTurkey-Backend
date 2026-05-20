using Business.Models;

namespace Business.Abstract;

/// <summary>
/// WorkflowEventBus publish anında RuleContext'i merkezi olarak zenginleştirir:
/// - SystemUserId varsa UserSnapshot + UserRole + InstitutionId doldurulur.
/// - login_failed gibi user-bilinmeyen olaylar için Metadata["AttemptedUserName"]
///   üzerinden lookup yapılır.
/// - ProblemId varsa ProblemSnapshot + ProblemStatus doldurulur.
/// - Bu sayede publish noktalarının her birinin tüm context alanlarını manuel
///   doldurma zorunluluğu kalkar (B5 + B6 düzeltmesi).
/// </summary>
public interface IRuleContextEnricher
{
    Task<RuleContext> EnrichAsync(RuleContext context);
}
