using System.Diagnostics;
using System.Text.Json;
using Business.Abstract;
using Business.Models;
using Core.CrossCuttingConcerns.Logging;
using Core.Utilities.Authorization;
using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// C# Node kodlarını izole bir child process içinde çalıştıran yönetici sınıf.
/// İletişim: stdin/stdout üzerinden JSON (SandboxRequest → SandboxResponse).
/// Güvenlik: expert.csharp_execute capability + kill switch kontrolü.
/// </summary>
public class RuleExecutionManager : IRuleExecutionService
{
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IKillSwitchService _killSwitch;
    private readonly ICapabilityResolver _resolver;
    private readonly ILogService _logService;
    private readonly ILogger<RuleExecutionManager> _logger;
    private readonly string _sandboxDllPath;

    public RuleExecutionManager(
        IKillSwitchService killSwitch,
        ICapabilityResolver resolver,
        ILogService logService,
        ILogger<RuleExecutionManager> logger,
        IConfiguration configuration)
    {
        _killSwitch = killSwitch;
        _resolver = resolver;
        _logService = logService;
        _logger = logger;
        _sandboxDllPath = ResolveSandboxPath(configuration);
    }

    /// <inheritdoc />
    public async Task<IDataResult<object?>> ExecuteCSharpNodeAsync(string csharpCode, RuleContext context)
    {
        var details = $"UserId={context.SystemUserId} InstitutionId={context.InstitutionId} Trigger={context.TriggerEventName}";

        // ─── KILL SWITCH KONTROLÜ ────────────────────────────────────────────────
        if (_killSwitch.IsSoft() || _killSwitch.IsHard() || _killSwitch.IsEmergency())
        {
            _logger.LogWarning(
                "[RuleExecution] Kill switch aktif — C# Node çalıştırma reddedildi. UserId={UserId}",
                context.SystemUserId);
            _logService.LogWarning("CSharpNode", "denied", "Kill switch aktif — çalıştırma reddedildi.", details, context.InstitutionId);
            return new ErrorDataResult<object?>(null, "Workflow altyapısı durduruldu (kill switch aktif).");
        }

        // ─── CAPABILITY KONTROLÜ ─────────────────────────────────────────────────
        // WorkflowCreatorId > 0 ise kontrol workflow yaratıcısına yapılır (ActionDispatcher ile tutarlı)
        var authUserId = context.WorkflowCreatorId > 0 ? context.WorkflowCreatorId : context.SystemUserId;
        if (!_resolver.Allows(authUserId, "expert.csharp_execute"))
        {
            _logger.LogWarning(
                "[RuleExecution] expert.csharp_execute capability eksik. AuthUserId={AuthUserId}",
                authUserId);
            _logService.LogWarning("CSharpNode", "denied", "expert.csharp_execute capability eksik.", details, context.InstitutionId);
            return new ErrorDataResult<object?>(null, "Bu işlem için 'expert.csharp_execute' yetkisi gereklidir.");
        }
        // ─────────────────────────────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(csharpCode))
            return new ErrorDataResult<object?>(null, "Çalıştırılacak C# kodu boş olamaz.");

        if (!File.Exists(_sandboxDllPath))
        {
            _logger.LogError("[RuleExecution] Sandbox DLL bulunamadı: {Path}", _sandboxDllPath);
            _logService.LogError("CSharpNode", "sandbox_missing", "CSharpSandbox.dll bulunamadı.",
                $"{details} | Path={_sandboxDllPath}", context.InstitutionId);
            return new ErrorDataResult<object?>(null, "C# sandbox yürütülebilir dosyası bulunamadı.");
        }

        var requestJson = JsonSerializer.Serialize(new { Code = csharpCode, Context = context });

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"exec \"{_sandboxDllPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RuleExecution] Sandbox process başlatılamadı. UserId={UserId}", context.SystemUserId);
            _logService.LogError("CSharpNode", "sandbox_start_error", "Process başlatma hatası.",
                $"{details} | {ex.Message}", context.InstitutionId);
            return new ErrorDataResult<object?>(null, $"C# sandbox başlatılamadı: {ex.Message}");
        }

        await process.StandardInput.WriteAsync(requestJson);
        process.StandardInput.Close();

        // Stdout/stderr okumayı process.Exit bekleme ile paralel başlat (deadlock önlemi)
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(ExecutionTimeout);

        _logger.LogInformation(
            "[RuleExecution] C# Node sandbox'ta çalıştırılıyor. UserId={UserId}, ProblemId={ProblemId}, TimeoutSec={TimeoutSec}",
            context.SystemUserId, context.ProblemId, ExecutionTimeout.TotalSeconds);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            _logger.LogWarning(
                "[RuleExecution] TIMEOUT — sandbox {TimeoutSec}s içinde tamamlanmadı. UserId={UserId}",
                ExecutionTimeout.TotalSeconds, context.SystemUserId);
            _logService.LogWarning("CSharpNode", "timeout",
                $"C# Node {ExecutionTimeout.TotalSeconds:0}s içinde tamamlanmadı.", details, context.InstitutionId);
            return new ErrorDataResult<object?>(null, $"C# Node zaman aşımına uğradı ({ExecutionTimeout.TotalSeconds:0}s).");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (string.IsNullOrWhiteSpace(stdout))
        {
            var errMsg = string.IsNullOrWhiteSpace(stderr) ? "Sandbox çıktısı boş." : stderr.Trim();
            _logger.LogError("[RuleExecution] Sandbox boş çıktı döndü. UserId={UserId}, Stderr={Stderr}",
                context.SystemUserId, stderr);
            _logService.LogError("CSharpNode", "runtime_error", "Sandbox boş çıktı döndü.",
                $"{details} | {errMsg}", context.InstitutionId);
            return new ErrorDataResult<object?>(null, $"C# Node çalışma zamanı hatası: {errMsg}");
        }

        SandboxResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<SandboxResponse>(stdout, _jsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[RuleExecution] Sandbox yanıtı ayrıştırılamadı. UserId={UserId}", context.SystemUserId);
            _logService.LogError("CSharpNode", "runtime_error", "Sandbox yanıt JSON hatası.", details, context.InstitutionId);
            return new ErrorDataResult<object?>(null, "C# sandbox yanıtı geçersiz JSON.");
        }

        if (response?.Success == true)
        {
            _logger.LogInformation(
                "[RuleExecution] C# Node başarıyla tamamlandı. UserId={UserId}, Result={Result}",
                context.SystemUserId, response.Result);
            _logService.LogInfo("CSharpNode", "executed", "C# Node başarıyla tamamlandı.", details, context.InstitutionId);
            return new SuccessDataResult<object?>(response.Result, "C# Node başarıyla çalıştırıldı.");
        }

        var errorType = response?.ErrorType ?? "runtime_error";
        var error = response?.Error ?? "Bilinmeyen hata.";

        if (errorType == "compile_error")
        {
            _logger.LogError("[RuleExecution] Derleme hatası. UserId={UserId}, Errors={Errors}",
                context.SystemUserId, error);
            _logService.LogError("CSharpNode", "compile_error", "C# Node derleme hatası.",
                $"{details} | {error}", context.InstitutionId);
            return new ErrorDataResult<object?>(null, $"C# Node derleme hatası: {error}");
        }

        _logger.LogError("[RuleExecution] Çalışma zamanı hatası. UserId={UserId}, Error={Error}",
            context.SystemUserId, error);
        _logService.LogError("CSharpNode", "runtime_error", "C# Node çalışma zamanı hatası.",
            $"{details} | {error}", context.InstitutionId);
        return new ErrorDataResult<object?>(null, $"C# Node çalışma zamanı hatası: {error}");
    }

    private static string ResolveSandboxPath(IConfiguration config)
    {
        var configured = config["CSharpSandbox:DllPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        }
        return Path.Combine(AppContext.BaseDirectory, "sandbox", "CSharpSandbox.dll");
    }

    // ── IPC types ──────────────────────────────────────────────────────────────

    private sealed class SandboxResponse
    {
        public bool Success { get; set; }
        public string? Result { get; set; }
        public string? Error { get; set; }
        public string? ErrorType { get; set; }
    }
}
