using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Entities.DTOs.Capability;

namespace Business.Concrete.Actions;

/// <summary>apply_capability_template — Yetki şablonunun son versiyonunu kullanıcıya uygular.</summary>
public class ApplyCapabilityTemplateActionHandler : IWorkflowActionHandler
{
    private readonly ICapabilityTemplateService _templateService;

    public string ActionCode => "apply_capability_template";

    public ApplyCapabilityTemplateActionHandler(ICapabilityTemplateService templateService)
    {
        _templateService = templateService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "templateId", ActionCode, errors);
        return errors;
    }

    public async Task<IDataResult<object?>> ExecuteAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var userId = WorkflowParameterResolver.ResolveUserId(parameters, context);
        if (userId <= 0)
            return new ErrorDataResult<object?>(null, "apply_capability_template: kullanıcı ID çözülemedi.");

        var templateIdRaw = parameters.GetValueOrDefault("templateId");
        if (!int.TryParse(templateIdRaw, out var templateId) || templateId <= 0)
            return new ErrorDataResult<object?>(null, "apply_capability_template: geçerli bir templateId gerekli.");

        var templateResult = _templateService.GetById(templateId);
        if (!templateResult.Success || templateResult.Data is null)
            return new ErrorDataResult<object?>(null,
                $"apply_capability_template: şablon bulunamadı (ID: {templateId}).");

        var template = templateResult.Data;
        if (!template.IsActive || template.LatestVersion is null)
            return new ErrorDataResult<object?>(null,
                $"apply_capability_template: şablon aktif değil veya yayınlanmış sürümü yok (ID: {templateId}).");

        var reason = WorkflowParameterResolver.Resolve(
            parameters.GetValueOrDefault("reason"), context);
        var expiresAtRaw = parameters.GetValueOrDefault("expiresAt");
        DateTime? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(expiresAtRaw) &&
            DateTime.TryParse(expiresAtRaw, out var parsed))
            expiresAt = parsed.ToUniversalTime();

        var applyResult = await _templateService.ApplyAsync(templateId, new ApplyTemplateDto
        {
            TemplateVersionId = template.LatestVersion.Id,
            UserIds           = new List<int> { userId },
            ExpiresAt         = expiresAt,
            Reason            = string.IsNullOrWhiteSpace(reason) ? "workflow_template_apply" : reason,
        });

        return applyResult.Success
            ? new SuccessDataResult<object?>(
                new { userId, templateId, versionId = template.LatestVersion.Id, templateName = template.Name },
                $"Şablon uygulandı: '{template.Name}' → kullanıcı {userId}.")
            : new ErrorDataResult<object?>(null,
                $"apply_capability_template başarısız: {applyResult.Message}");
    }
}
