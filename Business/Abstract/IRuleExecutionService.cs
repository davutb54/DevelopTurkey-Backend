using Business.Models;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// Roslyn C# Script motorunu kullanarak dinamik kural kodlarını çalıştıran servis arayüzü.
/// </summary>
public interface IRuleExecutionService
{
    /// <summary>
    /// Verilen C# kodunu Roslyn Script motoru ile asenkron olarak çalıştırır.
    /// Güvenlik kontrolü: Yalnızca SuperAdmin rolündeki kullanıcıların kaydettiği kurallar çalıştırılır.
    /// </summary>
    /// <param name="csharpCode">Çalıştırılacak C# kodu (script formatında).</param>
    /// <param name="context">Kural çalışma bağlamı (kullanıcı, problem, kurum vb. bilgileri).</param>
    /// <returns>Başarı/hata durumu ve varsa dönen değer.</returns>
    Task<IDataResult<object?>> ExecuteCSharpNodeAsync(string csharpCode, RuleContext context);
}
