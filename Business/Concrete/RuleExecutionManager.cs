using Business.Abstract;
using Business.Models;
using Core.CrossCuttingConcerns.Logging;
using Core.Utilities.Results;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// Roslyn C# Script motorunu kullanarak dinamik kural (C# Node) kodlarını çalıştıran yönetici sınıf.
/// Güvenlik: Yalnızca SuperAdmin rolündeki kullanıcıların kaydettiği kurallar çalıştırılır.
/// </summary>
public class RuleExecutionManager : IRuleExecutionService
{
    private const string SuperAdminRole = "Admin";
    private const int SuperAdminUserId = 2;

    private readonly ILogger<RuleExecutionManager> _logger;

    // Roslyn script seçenekleri: izin verilen assembly referansları ve namespace'ler
    private static readonly ScriptOptions _scriptOptions = ScriptOptions.Default
        .WithReferences(
            typeof(object).Assembly,                          // mscorlib / System.Private.CoreLib
            typeof(Enumerable).Assembly,                      // System.Linq
            typeof(System.Text.StringBuilder).Assembly,       // System.Text
            typeof(System.Collections.Generic.List<>).Assembly
        )
        .WithImports(
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Text"
        );

    public RuleExecutionManager(ILogger<RuleExecutionManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IDataResult<object?>> ExecuteCSharpNodeAsync(string csharpCode, RuleContext context)
    {
        // ─── GÜVENLİK KONTROLÜ ───────────────────────────────────────────────────
        // Yalnızca SuperAdmin rolündeki veya Id == 1 olan kullanıcıların
        // kaydettiği C# Node kodları çalıştırılır.
        if (!IsSuperAdmin(context))
        {
            _logger.LogWarning(
                "[RuleExecution] Yetkisiz erişim denemesi. UserId={UserId}, Role={Role}",
                context.SystemUserId, context.UserRole);

            return new ErrorDataResult<object?>(
                null,
                "Bu işlem yalnızca Süper Admin yetkisiyle gerçekleştirilebilir.");
        }
        // ─────────────────────────────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(csharpCode))
        {
            return new ErrorDataResult<object?>(null, "Çalıştırılacak C# kodu boş olamaz.");
        }

        try
        {
            _logger.LogInformation(
                "[RuleExecution] C# Node çalıştırılıyor. UserId={UserId}, ProblemId={ProblemId}, ExecutedAt={ExecutedAt}",
                context.SystemUserId, context.ProblemId, context.ExecutedAt);

            // Roslyn ile kodu çalıştır; globals olarak RuleContext nesnesini aktar
            object? result = await CSharpScript.EvaluateAsync<object>(
                code: csharpCode,
                options: _scriptOptions,
                globals: context,
                globalsType: typeof(RuleContext));

            _logger.LogInformation(
                "[RuleExecution] C# Node başarıyla tamamlandı. UserId={UserId}, Result={Result}",
                context.SystemUserId, result);

            return new SuccessDataResult<object?>(result, "C# Node başarıyla çalıştırıldı.");
        }
        catch (CompilationErrorException compilationEx)
        {
            // Derleme hatası (sözdizimi / tip hatası vb.)
            var errorMessages = string.Join(Environment.NewLine, compilationEx.Diagnostics);

            _logger.LogError(
                compilationEx,
                "[RuleExecution] Derleme hatası. UserId={UserId}, Errors={Errors}",
                context.SystemUserId, errorMessages);

            return new ErrorDataResult<object?>(
                null,
                $"C# Node derleme hatası: {errorMessages}");
        }
        catch (Exception ex)
        {
            // Çalışma zamanı hatası
            _logger.LogError(
                ex,
                "[RuleExecution] Çalışma zamanı hatası. UserId={UserId}, Message={Message}",
                context.SystemUserId, ex.Message);

            return new ErrorDataResult<object?>(
                null,
                $"C# Node çalışma zamanı hatası: {ex.Message}");
        }
    }

    // ─── YARDIMCI METOTLAR ────────────────────────────────────────────────────

    /// <summary>
    /// Kullanıcının SuperAdmin yetkisine sahip olup olmadığını kontrol eder.
    /// SuperAdmin: Rolü "SuperAdmin" olan VEYA UserId == 1 olan kullanıcılar.
    /// </summary>
    private static bool IsSuperAdmin(RuleContext context)
    {
        return context.UserRole?.Equals(SuperAdminRole, StringComparison.OrdinalIgnoreCase) == true
               || context.SystemUserId == SuperAdminUserId;
    }
}
