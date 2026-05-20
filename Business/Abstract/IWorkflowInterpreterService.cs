using Business.Models;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// Kaydedilen FlowJson'u okuyup workflow grafiğini çalıştıran yorumlayıcı servis.
/// </summary>
public interface IWorkflowInterpreterService
{
    /// <summary>
    /// ReactFlow tabanlı FlowJson'u çözümler ve graph traversal ile ilgili node'ları çalıştırır.
    /// </summary>
    /// <param name="flowJson">ReactFlow nodes + edges JSON'u.</param>
    /// <param name="context">Kural çalıştırma bağlamı.</param>
    /// <returns>Çalıştırma sonucu ve varsa trace bilgisi.</returns>
    Task<IDataResult<object?>> ExecuteWorkflowAsync(string flowJson, RuleContext context);
}