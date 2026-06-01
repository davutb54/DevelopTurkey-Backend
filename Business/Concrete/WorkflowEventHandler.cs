using System.Diagnostics;
using System.Text.Json;
using Business.Abstract;
using Business.Constants;
using Business.Models;
using Entities.Concrete;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// Tüm workflow eventlerini dinler; cache'den aktif kuralları alır ve
/// her kural için ayrı bir DI scope'unda arka planda iş akışını çalıştırır.
/// Execution sonucu WorkflowLog tablosuna yazılır.
/// </summary>
public sealed class WorkflowEventHandler : IWorkflowEventHandler
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// İ7: Tek bir kural için maksimum execution süresi. CSharp script veya
    /// webhook gibi blocking action'lar sonsuza kadar hang etmesin. Bu süre
    /// aşılırsa kural "timeout" status'u ile loglanır ve traversal iptal edilir.
    /// </summary>
    private static readonly TimeSpan RuleExecutionTimeout = TimeSpan.FromSeconds(30);

    private readonly IDynamicRuleService _dynamicRuleService;
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKillSwitchService _killSwitch;
    private readonly ILogger<WorkflowEventHandler> _logger;

    public WorkflowEventHandler(
        IDynamicRuleService dynamicRuleService,
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        IKillSwitchService killSwitch,
        ILogger<WorkflowEventHandler> logger)
    {
        _dynamicRuleService = dynamicRuleService;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _killSwitch = killSwitch;
        _logger = logger;
    }

    public async Task HandleAsync(string eventName, RuleContext context)
    {
        if (_killSwitch.IsSoft())
        {
            _logger.LogWarning(
                "[WorkflowEventHandler] Kill switch aktif — event işlenmedi. Event={Event}", eventName);
            return;
        }

        var institutionId = context.InstitutionId ?? 0;
        var rules = await GetRulesAsync(eventName, institutionId);

        if (rules.Count == 0)
        {
            _logger.LogDebug(
                "[WorkflowEventHandler] Aktif kural yok. Event={Event}, Institution={Institution}",
                eventName, institutionId);
            return;
        }

        _logger.LogInformation(
            "[WorkflowEventHandler] {Count} kural tetikleniyor. Event={Event}, Institution={Institution}",
            rules.Count, eventName, institutionId);

        // İ2: Kuralları priority sırasına göre SEQUENTIAL çalıştır (tek Task.Run içinde
        // foreach + await). Aksi halde paralel başlatılan task'ler priority'yi anlamsız
        // kılar. HTTP yanıt yine bekletilmez (Task.Run fire-and-forget).
        FireAndForgetSequence(rules, context);
    }

    // ── Cache-backed rule lookup ─────────────────────────────────────────────

    private async Task<List<DynamicRule>> GetRulesAsync(string eventName, int institutionId)
    {
        var cacheKey = WorkflowCacheKeys.Rules(institutionId, eventName);

        if (_cache.TryGetValue(cacheKey, out List<DynamicRule>? cached) && cached is not null)
            return cached;

        var result = await _dynamicRuleService.GetByTriggerEventAsync(eventName, institutionId);
        var rules = (result.Success ? result.Data : null) ?? [];

        // Determinizm: eşit Priority değerinde tie-break Id ile (eski kural önce çalışır)
        rules = [.. rules.OrderBy(r => r.Priority).ThenBy(r => r.Id)];

        _cache.Set(cacheKey, rules, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheTtl
        });

        _logger.LogDebug(
            "[WorkflowEventHandler] Cache miss → {Count} kural DB'den yüklendi. Event={Event}, Institution={Institution}",
            rules.Count, eventName, institutionId);

        return rules;
    }

    // ── Background execution ─────────────────────────────────────────────────

    /// <summary>
    /// Tüm kuralları tek bir background task içinde sıralı çalıştırır — priority
    /// order'ı deterministik kılmak için.
    /// </summary>
    private void FireAndForgetSequence(List<DynamicRule> rules, RuleContext context)
    {
        _ = Task.Run(async () =>
        {
            foreach (var rule in rules)
            {
                await ExecuteSingleRuleAsync(rule, context);
            }
        });
    }

    private void FireAndForget(DynamicRule rule, RuleContext context)
    {
        // Backwards-compatible — internal kullanım için (legacy testlere yedek).
        _ = Task.Run(() => ExecuteSingleRuleAsync(rule, context));
    }

    private async Task ExecuteSingleRuleAsync(DynamicRule rule, RuleContext context)
    {
        var ruleId        = rule.Id;
        var ruleName      = rule.Name;
        var flowJson      = rule.FlowJson;
        var institutionId = rule.InstitutionId;
        var triggerEvent  = rule.TriggerEvent;
        var userId        = context.SystemUserId;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var interpreter    = scope.ServiceProvider.GetRequiredService<IWorkflowInterpreterService>();
        var workflowLogSvc = scope.ServiceProvider.GetRequiredService<IWorkflowLogService>();

        var sw = Stopwatch.StartNew();
        string status      = "error";
        string? errorMsg   = null;
        string? traceJson  = null;
        int totalNodes     = 0;
        int executedNodes  = 0;

        try
        {
                _logger.LogInformation(
                    "[WorkflowEventHandler] Kural çalıştırılıyor. RuleId={RuleId} ({RuleName})",
                    ruleId, ruleName);

                // Capability kontrolü workflow'u yaratan admin üzerinden yapılsın;
                // tetikleyen kullanıcı (örn. yeni kayıt olan) yetersiz yetkiye sahip olabilir.
                context.WorkflowCreatorId = rule.CreatedByUserId;

                // İ7: Timeout — interpreter sonsuza dek hung kalmasın.
                using var cts = new CancellationTokenSource(RuleExecutionTimeout);
                var execTask = interpreter.ExecuteWorkflowAsync(flowJson, context);
                var winner = await Task.WhenAny(execTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (winner != execTask)
                {
                    sw.Stop();
                    status   = "timeout";
                    errorMsg = $"Kural {RuleExecutionTimeout.TotalSeconds:0}s içinde tamamlanmadı.";
                    _logger.LogWarning(
                        "[WorkflowEventHandler] RuleId={RuleId} TIMEOUT (>{Sec}s)",
                        ruleId, RuleExecutionTimeout.TotalSeconds);
                    return;
                }
                var execResult = await execTask;
                sw.Stop();

                if (execResult.Success && execResult.Data is not null)
                {
                    // İ5: ExecuteWorkflowAsync artık WorkflowExecutionSummary döner.
                    // Direkt cast ile çift JSON dönüşümünden kaçınılır; eski
                    // anonymous-object dönüşler için fallback deserialize korunur.
                    var summary = execResult.Data as WorkflowExecutionSummary;
                    if (summary == null)
                    {
                        summary = JsonSerializer.Deserialize<WorkflowExecutionSummary>(
                            JsonSerializer.Serialize(execResult.Data),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }

                    totalNodes    = summary?.NodeCount    ?? 0;
                    executedNodes = summary?.VisitedNodeCount ?? 0;
                    var trace     = summary?.Trace ?? new();
                    var errs      = summary?.Errors ?? new();

                    traceJson = JsonSerializer.Serialize(trace);

                    status = (summary?.HasErrors ?? errs.Count > 0) ? "partial" : "success";

                    if (errs.Count > 0)
                    {
                        // Yapısal hata mesajlarını WorkflowLog.ErrorMessage'a yansıt — ilk
                        // 3 tanesi yeterli (UI'da gösterim için kısa tutulur).
                        // İ4: Toplam mesaj uzunluğunu da sınırla (~1000 char) — uzun
                        // exception mesajlarının log tablosunu / UI'ı bozmasını önler.
                        errorMsg = string.Join(" | ", errs.Take(3));
                        const int MaxErrorMessageLength = 1000;
                        if (errorMsg.Length > MaxErrorMessageLength)
                        {
                            errorMsg = errorMsg.Substring(0, MaxErrorMessageLength - 3) + "...";
                        }
                    }

                    _logger.LogInformation(
                        "[WorkflowEventHandler] Kural tamamlandı. RuleId={RuleId} | Status={Status} | Errors={ErrorCount} | {Ms}ms",
                        ruleId, status, errs.Count, sw.ElapsedMilliseconds);
                }
                else
                {
                    sw.Stop();
                    status   = "failed";
                    errorMsg = execResult.Message;

                    _logger.LogWarning(
                        "[WorkflowEventHandler] Kural başarısız. RuleId={RuleId} | {Message}",
                        ruleId, execResult.Message);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                status   = "error";
                errorMsg = ex.Message;

                _logger.LogError(ex,
                    "[WorkflowEventHandler] RuleId={RuleId} ({RuleName}) çalıştırılırken hata.",
                    ruleId, ruleName);
            }
            finally
            {
                try
                {
                    workflowLogSvc.Add(new WorkflowLog
                    {
                        InstitutionId     = institutionId,
                        RuleId            = ruleId,
                        RuleName          = ruleName,
                        TriggerEvent      = triggerEvent,
                        TriggeredByUserId = userId,
                        Status            = status,
                        ErrorMessage      = errorMsg,
                        TraceJson         = traceJson,
                        TotalNodeCount    = totalNodes,
                        ExecutedNodeCount = executedNodes,
                        DurationMs        = sw.ElapsedMilliseconds,
                        ExecutedAt        = DateTime.Now,
                    });
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx,
                        "[WorkflowEventHandler] Execution log kaydedilemedi. RuleId={RuleId}", ruleId);
                }
            }
    }

    // İ5 sonrası: özel ExecutionSummary class'ı kaldırıldı; Business.Models.WorkflowExecutionSummary kullanılıyor.
}
