namespace Business.Models;

public sealed record WebhookSendResult(bool Success, int StatusCode, string? ErrorMessage);
