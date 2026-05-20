using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Business.Abstract;
using Business.Models;

namespace WebAPI.Services;

public sealed class WebhookClient : IWebhookClient
{
    private readonly HttpClient _httpClient;

    public WebhookClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WebhookSendResult> SendAsync(
        string url,
        string method,
        string payload,
        string? authHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new WebhookSendResult(false, 0, "Webhook URL bos olamaz.");
        }

        var httpMethod = new HttpMethod(string.IsNullOrWhiteSpace(method) ? "POST" : method.ToUpperInvariant());
        using var request = new HttpRequestMessage(httpMethod, url);

        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        if (httpMethod != HttpMethod.Get)
        {
            request.Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? new WebhookSendResult(true, (int)response.StatusCode, null)
                : new WebhookSendResult(false, (int)response.StatusCode, "HTTP " + (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return new WebhookSendResult(false, 0, ex.Message);
        }
    }
}
