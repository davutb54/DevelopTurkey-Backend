using Business.Concrete;
using Core.Utilities.Authorization;
using Moq;

namespace DevelopTurkey.Workflow.Tests.Capability;

/// <summary>
/// CapabilityResolver scope match senaryoları (6 senaryo).
/// Mock ICapabilitySnapshot kullanılır — DB'ye gitmez.
/// </summary>
public sealed class CapabilityResolverTests
{
    // ─── Yardımcı fabrika ─────────────────────────────────────────────────

    private static ICapabilityResolver BuildResolver(
        params UserCapabilitySnapshotEntry[] entries)
    {
        var snapshot = new Mock<ICapabilitySnapshot>();
        snapshot.Setup(s => s.Get(It.IsAny<int>()))
                .Returns((int uid) => entries.Where(e => e.UserId == uid).ToList());
        return new CapabilityResolver(snapshot.Object);
    }

    private static UserCapabilitySnapshotEntry GlobalEntry(
        int userId = 1,
        string code = "admin.test",
        DateTime? expiresAt = null,
        int? institutionId = null,
        string? scopeJson = null) =>
        new(Id: 1, UserId: userId, CapabilityId: 10,
            CapabilityCode: code, InstitutionId: institutionId,
            ScopeJson: scopeJson, ExpiresAt: expiresAt);

    // ─── Senaryo 1: Global capability eşleşmesi (scope null) → allow ──────

    [Fact]
    public void Allows_GlobalCapability_NoContext_ReturnsTrue()
    {
        var resolver = BuildResolver(GlobalEntry(userId: 1, code: "admin.test"));

        resolver.Allows(1, "admin.test").Should().BeTrue();
    }

    // ─── Senaryo 2: Institution scope tam eşleşmesi → allow ──────────────

    [Fact]
    public void Allows_InstitutionScopeMatch_ReturnsTrue()
    {
        var entry = GlobalEntry(userId: 1, code: "moderation.problem_delete",
                                institutionId: 42);
        var resolver = BuildResolver(entry);
        var ctx = new CapabilityRequestContext(InstitutionId: 42);

        resolver.Allows(1, "moderation.problem_delete", ctx).Should().BeTrue();
    }

    // ─── Senaryo 3: Institution scope uyuşmazlığı → deny ─────────────────

    [Fact]
    public void Allows_InstitutionScopeMismatch_ReturnsFalse()
    {
        var entry = GlobalEntry(userId: 1, code: "moderation.problem_delete",
                                institutionId: 42);
        var resolver = BuildResolver(entry);
        var ctx = new CapabilityRequestContext(InstitutionId: 99); // yanlış kurum

        resolver.Allows(1, "moderation.problem_delete", ctx).Should().BeFalse();
    }

    // ─── Senaryo 4: Entity scope eşleşmesi → allow ───────────────────────

    [Fact]
    public void Allows_EntityScopeMatch_ReturnsTrue()
    {
        const string scopeJson = """{"entity":"problem","entityId":null}""";
        var entry = GlobalEntry(userId: 1, code: "moderation.problem_delete",
                                scopeJson: scopeJson);
        var resolver = BuildResolver(entry);
        var ctx = new CapabilityRequestContext(Entity: "problem");

        resolver.Allows(1, "moderation.problem_delete", ctx).Should().BeTrue();
    }

    // ─── Senaryo 5: EntityId scope uyuşmazlığı → deny ────────────────────

    [Fact]
    public void Allows_EntityIdScopeMismatch_ReturnsFalse()
    {
        const string scopeJson = """{"entity":"problem","entityId":100}""";
        var entry = GlobalEntry(userId: 1, code: "moderation.problem_delete",
                                scopeJson: scopeJson);
        var resolver = BuildResolver(entry);
        var ctx = new CapabilityRequestContext(Entity: "problem", EntityId: 999);

        resolver.Allows(1, "moderation.problem_delete", ctx).Should().BeFalse();
    }

    // ─── Senaryo 6: Süresi dolmuş capability → deny ──────────────────────

    [Fact]
    public void Allows_ExpiredCapability_ReturnsFalse()
    {
        var entry = GlobalEntry(userId: 1, code: "user.problem_create",
                                expiresAt: DateTime.UtcNow.AddHours(-1)); // geçmişte
        var resolver = BuildResolver(entry);

        resolver.Allows(1, "user.problem_create").Should().BeFalse();
    }

    // ─── Ek: Capability hiç yok → "no_capability" reason ────────────────

    [Fact]
    public void Resolve_NoCapability_ReturnsNoCapabilityReason()
    {
        var resolver = BuildResolver(); // boş snapshot

        var result = resolver.Resolve(1, "admin.test");

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Be("no_capability");
    }

    // ─── Ek: GetEffectiveCodes süresi dolmuşları filtreler ───────────────

    [Fact]
    public void GetEffectiveCodes_ExcludesExpiredEntries()
    {
        var active  = GlobalEntry(userId: 1, code: "user.problem_create");
        var expired = GlobalEntry(userId: 1, code: "user.solution_create",
                                  expiresAt: DateTime.UtcNow.AddMinutes(-5));
        var resolver = BuildResolver(active, expired);

        var codes = resolver.GetEffectiveCodes(1);

        codes.Should().Contain("user.problem_create");
        codes.Should().NotContain("user.solution_create");
    }
}
