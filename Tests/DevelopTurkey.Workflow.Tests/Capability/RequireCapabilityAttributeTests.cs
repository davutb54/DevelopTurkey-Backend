using Core.Utilities.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using WebAPI.Filters;

namespace DevelopTurkey.Workflow.Tests.Capability;

/// <summary>
/// RequireCapabilityAttribute senaryoları (3 senaryo):
///   1. Anonim → 401 Unauthorized
///   2. Kimlik doğrulanmış, capability yok → 403 Forbidden
///   3. Kimlik doğrulanmış, capability var → filtre geçer (Result = null)
/// </summary>
public sealed class RequireCapabilityAttributeTests
{
    private const string TestCode = "admin.test_capability";

    // ─── Yardımcılar ──────────────────────────────────────────────────────

    private static AuthorizationFilterContext BuildContext(
        bool isAuthenticated,
        bool policyAllows)
    {
        // ICapabilityPolicy mock'u
        var policy = new Mock<ICapabilityPolicy>();
        policy.Setup(p => p.Allows(It.IsAny<string>(), It.IsAny<CapabilityRequestContext?>()))
              .Returns(policyAllows);

        // ServiceProvider → filtre içindeki RequestServices.GetService<ICapabilityPolicy>()
        var services = new ServiceCollection();
        services.AddSingleton(policy.Object);
        var provider = services.BuildServiceProvider();

        // HttpContext
        var httpContext = new DefaultHttpContext { RequestServices = provider };

        if (isAuthenticated)
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
                authenticationType: "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        // else: User kimlik doğrulaması yapılmamış (IsAuthenticated = false)

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    // ─── Senaryo 1: Anonim kullanıcı → 401 ───────────────────────────────

    [Fact]
    public async Task OnAuthorizationAsync_AnonymousUser_Returns401()
    {
        var attr = new RequireCapabilityAttribute(TestCode);
        var ctx  = BuildContext(isAuthenticated: false, policyAllows: false);

        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── Senaryo 2: Kimlik doğrulanmış, capability yok → 403 ─────────────

    [Fact]
    public async Task OnAuthorizationAsync_AuthenticatedNoCapability_Returns403()
    {
        var attr = new RequireCapabilityAttribute(TestCode);
        var ctx  = BuildContext(isAuthenticated: true, policyAllows: false);

        await attr.OnAuthorizationAsync(ctx);

        var objectResult = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);

        // Anonymous type — cross-assembly dynamic erişimi çalışmaz; JSON üzerinden doğrula
        var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
        json.Should().Contain(TestCode);
    }

    // ─── Senaryo 3: Kimlik doğrulanmış, capability var → geçer ───────────

    [Fact]
    public async Task OnAuthorizationAsync_AuthenticatedWithCapability_SetsNoResult()
    {
        var attr = new RequireCapabilityAttribute(TestCode);
        var ctx  = BuildContext(isAuthenticated: true, policyAllows: true);

        await attr.OnAuthorizationAsync(ctx);

        // Filtre Result'ı set etmemeli — pipeline devam eder
        ctx.Result.Should().BeNull();
    }
}
