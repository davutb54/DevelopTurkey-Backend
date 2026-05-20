using Business.Abstract;
using Business.Constants;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Concrete;

public class DynamicRuleManager : IDynamicRuleService
{
    private readonly IDynamicRuleDal _dynamicRuleDal;
    private readonly IClientContext _clientContext;
    private readonly IMemoryCache _cache;

    public DynamicRuleManager(
        IDynamicRuleDal dynamicRuleDal,
        IClientContext clientContext,
        IMemoryCache cache)
    {
        _dynamicRuleDal = dynamicRuleDal;
        _clientContext = clientContext;
        _cache = cache;
    }

    public IDataResult<DynamicRule> GetById(int id)
    {
        var result = _dynamicRuleDal.Get(d => d.Id == id);
        if (result == null) return new ErrorDataResult<DynamicRule>(default, "Dinamik kural bulunamadı.");
        return new SuccessDataResult<DynamicRule>(result);
    }

    public IDataResult<List<DynamicRule>> GetAll()
    {
        return new SuccessDataResult<List<DynamicRule>>(_dynamicRuleDal.GetAll());
    }

    public IDataResult<List<DynamicRule>> GetByInstitutionId(int institutionId)
    {
        return new SuccessDataResult<List<DynamicRule>>(_dynamicRuleDal.GetAll(d => d.InstitutionId == institutionId));
    }

    public Task<IDataResult<List<DynamicRule>>> GetActiveByInstitutionAsync(int institutionId)
    {
        var rules = _dynamicRuleDal.GetAll(d => d.InstitutionId == institutionId && d.IsActive);
        return Task.FromResult<IDataResult<List<DynamicRule>>>(new SuccessDataResult<List<DynamicRule>>(rules));
    }

    public Task<IDataResult<List<DynamicRule>>> GetByTriggerEventAsync(string triggerEventName, int institutionId)
    {
        var rules = _dynamicRuleDal.GetAll(d => d.InstitutionId == institutionId
                                               && d.TriggerEvent == triggerEventName
                                               && d.IsActive);
        return Task.FromResult<IDataResult<List<DynamicRule>>>(new SuccessDataResult<List<DynamicRule>>(rules));
    }

    public Task<IDataResult<DynamicRule>> SaveWorkflowAsync(SaveWorkflowDto dto)
    {
        if (dto == null)
        {
            return Task.FromResult<IDataResult<DynamicRule>>(
                new ErrorDataResult<DynamicRule>(default, "Workflow bilgisi boş."));
        }

        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var now = DateTime.UtcNow;

        // ── Mevcut kural güncelleme (ID verilmişse) ──────────────────────────
        if (dto.Id.HasValue && dto.Id.Value > 0)
        {
            var existing = _dynamicRuleDal.Get(d => d.Id == dto.Id.Value);
            if (existing == null)
            {
                return Task.FromResult<IDataResult<DynamicRule>>(
                    new ErrorDataResult<DynamicRule>(default, "Güncellenecek kural bulunamadı."));
            }

            var oldTrigger = existing.TriggerEvent;

            existing.Name         = dto.Name;
            existing.TriggerEvent = dto.TriggerEvent;
            existing.FlowJson     = dto.FlowJson;
            existing.Priority     = dto.Priority;
            existing.Description  = dto.Description;
            existing.IsActive     = dto.IsActive;
            existing.Version     += 1;
            existing.UpdatedAt    = now;

            _dynamicRuleDal.Update(existing);

            _cache.Remove(WorkflowCacheKeys.Rules(institutionId, oldTrigger));
            _cache.Remove(WorkflowCacheKeys.Rules(institutionId, dto.TriggerEvent));

            return Task.FromResult<IDataResult<DynamicRule>>(
                new SuccessDataResult<DynamicRule>(existing, $"Workflow v{existing.Version} olarak güncellendi."));
        }

        // ── Yeni kural oluşturma (ID yoksa) ──────────────────────────────────
        int createdByUserId = _clientContext.GetUserId() ?? 0;

        var latest = _dynamicRuleDal
            .GetAll(d => d.InstitutionId == institutionId
                         && d.Name == dto.Name
                         && d.TriggerEvent == dto.TriggerEvent)
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        if (latest != null && latest.IsActive && dto.IsActive)
        {
            latest.IsActive = false;
            latest.UpdatedAt = now;
            _dynamicRuleDal.Update(latest);
        }

        var rule = new DynamicRule
        {
            InstitutionId    = institutionId,
            Name             = dto.Name,
            TriggerEvent     = dto.TriggerEvent,
            FlowJson         = dto.FlowJson,
            Version          = (latest?.Version ?? 0) + 1,
            Priority         = dto.Priority,
            Description      = dto.Description,
            CreatedAt        = now,
            UpdatedAt        = now,
            CreatedByUserId  = createdByUserId,
            IsActive         = dto.IsActive
        };

        _dynamicRuleDal.Add(rule);
        _cache.Remove(WorkflowCacheKeys.Rules(institutionId, dto.TriggerEvent));

        var message = latest == null ? "Workflow eklendi." : "Workflow yeni versiyonla güncellendi.";

        return Task.FromResult<IDataResult<DynamicRule>>(new SuccessDataResult<DynamicRule>(rule, message));
    }

    public IResult Add(DynamicRule dynamicRule)
    {
        _dynamicRuleDal.Add(dynamicRule);
        return new SuccessResult("Dinamik kural eklendi.");
    }

    public IResult Update(DynamicRule dynamicRule)
    {
        // İ3 fix: TriggerEvent değiştirilmiş olabilir; DB'deki eski değerle de
        // cache invalidation yapılmalı (aksi halde eski trigger için cache TTL
        // dolana kadar yanlış kural seti döner).
        var existing = _dynamicRuleDal.Get(d => d.Id == dynamicRule.Id);
        var oldTrigger = existing?.TriggerEvent;

        _dynamicRuleDal.Update(dynamicRule);

        _cache.Remove(WorkflowCacheKeys.Rules(dynamicRule.InstitutionId, dynamicRule.TriggerEvent));
        if (!string.IsNullOrEmpty(oldTrigger) &&
            !string.Equals(oldTrigger, dynamicRule.TriggerEvent, StringComparison.OrdinalIgnoreCase))
        {
            _cache.Remove(WorkflowCacheKeys.Rules(dynamicRule.InstitutionId, oldTrigger));
        }

        return new SuccessResult("Dinamik kural güncellendi.");
    }

    public IResult Delete(int id)
    {
        var result = _dynamicRuleDal.Get(d => d.Id == id);
        if (result == null) return new ErrorResult("Dinamik kural bulunamadı.");
        _dynamicRuleDal.Delete(result);
        _cache.Remove(WorkflowCacheKeys.Rules(result.InstitutionId, result.TriggerEvent));
        return new SuccessResult("Dinamik kural silindi.");
    }
}
