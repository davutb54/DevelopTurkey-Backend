using Business.Concrete;
using Core.Utilities.Context;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Linq.Expressions;

namespace DevelopTurkey.Workflow.Tests.Manager;

/// <summary>
/// WorkflowCacheKeys internal — bu format string'i ile aynı key'i üreten yardımcı.
/// Production kodda değişirse bu testler de güncellenmeli (kasıtlı tight coupling
/// — cache invalidation'ın doğru anahtarı temizlediğini doğrulamak için).
/// </summary>
internal static class CacheKeyHelper
{
    public static string Rules(int institutionId, string eventName) =>
        $"WorkflowRules_{institutionId}_{eventName}";
}

/// <summary>
/// DynamicRuleManager cache invalidation davranışı.
/// İ3 fix: Update() metodu eski TriggerEvent için de cache.Remove yapmalı.
/// </summary>
public sealed class DynamicRuleManagerCacheTests
{
    private static (DynamicRuleManager mgr, Mock<IDynamicRuleDal> dal, MemoryCache cache)
        BuildHarness(DynamicRule? existing = null)
    {
        var dal = new Mock<IDynamicRuleDal>();
        if (existing != null)
        {
            dal.Setup(d => d.Get(It.IsAny<Expression<Func<DynamicRule, bool>>>())).Returns(existing);
        }
        var ctx = new Mock<IClientContext>();
        ctx.Setup(c => c.GetInstitutionId()).Returns(1);
        ctx.Setup(c => c.GetUserId()).Returns(2);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mgr = new DynamicRuleManager(dal.Object, ctx.Object, cache);
        return (mgr, dal, cache);
    }

    [Fact]
    public void Update_With_Same_TriggerEvent_Invalidates_Single_Cache_Key()
    {
        var existing = new DynamicRule { Id = 1, InstitutionId = 1, TriggerEvent = "evt.same", IsActive = true };
        var (mgr, _, cache) = BuildHarness(existing);

        // Cache'i önceden doldur
        var keyA = CacheKeyHelper.Rules(1, "evt.same");
        cache.Set(keyA, new List<DynamicRule>() { existing });

        var updated = new DynamicRule { Id = 1, InstitutionId = 1, TriggerEvent = "evt.same", IsActive = true };
        mgr.Update(updated);

        cache.TryGetValue(keyA, out _).Should().BeFalse("aynı trigger için cache temizlenmeli");
    }

    [Fact]
    public void I3_Update_With_New_TriggerEvent_Invalidates_Both_Old_And_New_Cache_Keys()
    {
        // İ3 fix kanıtı: kullanıcı kuralın TriggerEvent'ini değiştirirse,
        // hem eski hem yeni event için cache temizlenmeli — aksi halde eski event
        // çalıştığında bu kural hala (yanlışlıkla) çalıştırılır.
        var existing = new DynamicRule { Id = 1, InstitutionId = 1, TriggerEvent = "evt.old", IsActive = true };
        var (mgr, _, cache) = BuildHarness(existing);

        var keyOld = CacheKeyHelper.Rules(1, "evt.old");
        var keyNew = CacheKeyHelper.Rules(1, "evt.new");
        cache.Set(keyOld, new List<DynamicRule> { existing });
        cache.Set(keyNew, new List<DynamicRule>());

        var updated = new DynamicRule { Id = 1, InstitutionId = 1, TriggerEvent = "evt.new", IsActive = true };
        mgr.Update(updated);

        cache.TryGetValue(keyOld, out _).Should().BeFalse("eski trigger cache'i de temizlenmeli (İ3 fix)");
        cache.TryGetValue(keyNew, out _).Should().BeFalse("yeni trigger cache'i temizlenmeli");
    }

    [Fact]
    public void Update_When_Existing_Not_Found_Still_Invalidates_New_Trigger_Cache()
    {
        // existing == null senaryosu: DAL'da kural bulunamaz (silinmiş olabilir).
        // Yine de verilen TriggerEvent için cache temizlenmeli.
        var (mgr, _, cache) = BuildHarness(existing: null);
        var keyNew = CacheKeyHelper.Rules(1, "evt.fresh");
        cache.Set(keyNew, new List<DynamicRule>());

        mgr.Update(new DynamicRule { Id = 99, InstitutionId = 1, TriggerEvent = "evt.fresh" });

        cache.TryGetValue(keyNew, out _).Should().BeFalse();
    }
}
