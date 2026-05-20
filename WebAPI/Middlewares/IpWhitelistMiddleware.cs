using Business.Abstract;
using System.Text.Json;

namespace WebAPI.Middlewares;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;

    public IpWhitelistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IInstitutionFeatureService institutionFeatureService, IInstitutionService institutionService)
    {
        int institutionId = 1;
        var institutionClaim = context.User?.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
        if (institutionClaim != null)
        {
            institutionId = int.Parse(institutionClaim.Value);
        }

        // Feature kontrolü
        bool isWhitelistEnabled = institutionFeatureService.IsFeatureEnabled(institutionId, "Identity.EnableIpWhitelist", false);
        bool isBlacklistEnabled = institutionFeatureService.IsFeatureEnabled(institutionId, "Identity.EnableIpBlacklist", false);

        if (!isWhitelistEnabled && !isBlacklistEnabled)
        {
            await _next(context);
            return;
        }

        // IP Kontrolleri (Whitelist & Blacklist)
        var institutionResult = institutionService.GetById(institutionId);
        if (institutionResult.Success && institutionResult.Data != null)
        {
            var customFieldsJson = institutionResult.Data.CustomFieldsJson;
            if (!string.IsNullOrEmpty(customFieldsJson))
            {
                try
                {
                    var customFields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(customFieldsJson);
                    if (customFields != null)
                    {
                        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
                        var normalizedIp = remoteIp == "::1" ? "127.0.0.1" : remoteIp;

                        // 1. ÖNCE BLACKLIST KONTROLÜ (Eğer özellik aktifse ve listedeyse direkt blokla)
                        if (isBlacklistEnabled && customFields.TryGetValue("BlacklistIps", out var blacklistElement))
                        {
                            var blacklistIps = JsonSerializer.Deserialize<List<string>>(blacklistElement.GetRawText());
                            if (blacklistIps != null && (blacklistIps.Contains(normalizedIp) || blacklistIps.Contains(remoteIp)))
                            {
                                context.Response.StatusCode = 403;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync("{\"success\":false,\"message\":\"IP adresiniz bu kurum için kara listededir.\"}");
                                return;
                            }
                        }

                        // 2. SONRA WHITELIST KONTROLÜ (Eğer özellik aktifse, whitelist doluysa ve listede değilse blokla)
                        if (isWhitelistEnabled && customFields.TryGetValue("WhitelistIps", out var whitelistElement))
                        {
                            var whitelistIps = JsonSerializer.Deserialize<List<string>>(whitelistElement.GetRawText());
                            if (whitelistIps != null && whitelistIps.Count > 0)
                            {
                                if (!whitelistIps.Contains(normalizedIp) && !whitelistIps.Contains(remoteIp))
                                {
                                    context.Response.StatusCode = 403;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync("{\"success\":false,\"message\":\"IP adresiniz bu kurum için yetkilendirilmemiş.\"}");
                                    return;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // JSON parse hatası durumunda akışı bozmamak için devam et
                }
            }
        }

        await _next(context);
    }
}