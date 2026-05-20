using System.Threading;
using System.Threading.Tasks;
using Business.Models;

namespace Business.Abstract;

public interface IWebhookClient
{
    Task<WebhookSendResult> SendAsync(
        string url,
        string method,
        string payload,
        string? authHeader,
        CancellationToken cancellationToken = default);
}
