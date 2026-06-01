using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Entities.DTOs.Capability;

namespace Business.Concrete.Actions;

/// <summary>grant_capability — Kullanıcıya yetki (capability) ver veya kaldır.</summary>
public class GrantCapabilityActionHandler : IWorkflowActionHandler
{
    private readonly IUserCapabilityService _userCapabilityService;

    public string ActionCode => "grant_capability";

    public GrantCapabilityActionHandler(IUserCapabilityService userCapabilityService)
    {
        _userCapabilityService = userCapabilityService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "capabilityCode", ActionCode, errors);
        WorkflowParameterResolver.RequireParam(parameters, "action", ActionCode, errors);
        return errors;
    }

    public async Task<IDataResult<object?>> ExecuteAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var userId = WorkflowParameterResolver.ResolveUserId(parameters, context);
        if (userId <= 0)
            return new ErrorDataResult<object?>(null, "grant_capability: kullanıcı ID çözülemedi.");

        var capabilityCode = WorkflowParameterResolver.Resolve(
            parameters.GetValueOrDefault("capabilityCode"), context);

        if (string.IsNullOrWhiteSpace(capabilityCode))
            return new ErrorDataResult<object?>(null, "grant_capability: capabilityCode boş.");

        var action = parameters.GetValueOrDefault("action", "grant");
        var reason = WorkflowParameterResolver.Resolve(
            parameters.GetValueOrDefault("reason"), context);
        var expiresAtRaw = parameters.GetValueOrDefault("expiresAt");
        DateTime? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(expiresAtRaw) &&
            DateTime.TryParse(expiresAtRaw, out var parsed))
            expiresAt = parsed.ToUniversalTime();

        if (action == "revoke")
        {
            var revokeResult = await _userCapabilityService.RevokeAsync(userId, new RevokeCapabilityDto
            {
                CapabilityCode = capabilityCode,
                Reason = string.IsNullOrWhiteSpace(reason) ? "workflow_revoke" : reason,
            });

            return revokeResult.Success
                ? new SuccessDataResult<object?>(new { userId, capabilityCode, action = "revoke" },
                    $"Yetki kaldırıldı: {capabilityCode} (kullanıcı {userId}).")
                : new ErrorDataResult<object?>(null,
                    $"grant_capability revoke başarısız: {revokeResult.Message}");
        }

        var grantResult = await _userCapabilityService.GrantAsync(userId, new GrantCapabilityDto
        {
            CapabilityCode = capabilityCode,
            ExpiresAt      = expiresAt,
            Reason         = string.IsNullOrWhiteSpace(reason) ? "workflow_grant" : reason,
        });

        return grantResult.Success
            ? new SuccessDataResult<object?>(new { userId, capabilityCode, action = "grant", expiresAt },
                $"Yetki verildi: {capabilityCode} (kullanıcı {userId}).")
            : new ErrorDataResult<object?>(null,
                $"grant_capability başarısız: {grantResult.Message}");
    }
}
