using System.Text.Json;
using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>
/// webhook — Dış sisteme HTTP isteği gönderme action'ı.
/// Geçici hatalarda (5xx / 429 / network) exponential backoff ile yeniden dener.
/// 4xx (kalıcı istemci hatası) için retry yapılmaz.
/// </summary>
public class WebhookActionHandler : IWorkflowActionHandler
{
    private const int MaxAttempts = 3;

    private readonly IWebhookClient _webhookClient;

    public string ActionCode => "webhook";

    public WebhookActionHandler(IWebhookClient webhookClient)
    {
        _webhookClient = webhookClient;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "url", ActionCode, errors);
        return errors;
    }

    public async Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var url        = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("url"),        context);
        var method     = (parameters.GetValueOrDefault("method") ?? "POST").ToUpperInvariant();
        var payload    = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("payload"),    context);
        var authHeader = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("authHeader"), context);

        if (string.IsNullOrWhiteSpace(url))
            return new ErrorDataResult<object?>(null, "webhook: 'url' parametresi boş.");

        try
        {
            // Otomatik context verisi payload yoksa oluştur
            if (string.IsNullOrWhiteSpace(payload))
            {
                payload = JsonSerializer.Serialize(new
                {
                    trigger      = context.TriggerEventName,
                    userId       = context.SystemUserId,
                    targetUserId = context.TargetUserId,
                    problemId    = context.ProblemId,
                    solutionId   = context.SolutionId,
                    timestamp    = context.ExecutedAt
                });
            }

            WebhookSendResult? result = null;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                result = await _webhookClient.SendAsync(url, method, payload, authHeader);

                if (result.Success)
                {
                    return new SuccessDataResult<object?>(
                        new { url, method, status = result.StatusCode, attempts = attempt },
                        attempt > 1
                            ? $"Webhook {attempt}. denemede tetiklendi."
                            : "Webhook başarıyla tetiklendi.");
                }

                // Kalıcı hata (4xx) → retry boşa gider
                if (!IsTransient(result.StatusCode)) break;

                if (attempt < MaxAttempts)
                {
                    var delayMs = 200 * (int)Math.Pow(3, attempt - 1);
                    await Task.Delay(delayMs);
                }
            }

            return new ErrorDataResult<object?>(
                null,
                $"Webhook başarısız ({MaxAttempts} deneme): " +
                $"{result?.ErrorMessage ?? "HTTP " + result?.StatusCode}");
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<object?>(null, $"Webhook hatası: {ex.Message}");
        }
    }

    /// <summary>Geçici hata mı (retry edilmeli)? 5xx / 429 / 0 (network).</summary>
    private static bool IsTransient(int statusCode)
        => statusCode == 0 || statusCode == 429 || (statusCode >= 500 && statusCode <= 599);
}
