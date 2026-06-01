using Business.Models;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// Action registry'de kayıtlı her workflow action handler'ın uygulaması gereken arayüz.
/// Her action kendi parametre doğrulamasını ve çalıştırma mantığını içerir.
/// </summary>
public interface IWorkflowActionHandler
{
    /// <summary>Bu handler'ın sorumlu olduğu action kodu (ör: "send_email").</summary>
    string ActionCode { get; }

    /// <summary>
    /// Parametrelerin geçerliliğini doğrular. Dispatcher çalıştırmadan önce çağırır.
    /// Boş liste = geçerli; dolu liste = hata mesajları.
    /// </summary>
    IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters);

    /// <summary>Parametreler doğrulandıktan sonra action'ı çalıştırır.</summary>
    Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context);
}
