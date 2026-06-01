using Business.Abstract;
using Business.Concrete;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Entities.DTOs;
using Entities.DTOs.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DevelopTurkey.Workflow.Tests.EventBus;

/// <summary>
/// RuleContextEnricher unit testleri (B5 + B6 fix kapsamı).
/// </summary>
public sealed class RuleContextEnricherTests
{
    private static UserDetailDto AdminUser(int id = 9012, int institutionId = 1) => new()
    {
        Id = id,
        UserName = "admin123",
        Name = "Admin",
        Surname = "User",
        Email = "admin@test",
        CityName = "Ankara",
        Gender = "M",
        AuthType = "Local",
        InstitutionId = institutionId
    };

    private static UserDetailDto ExpertUser(int id = 100) => new()
    {
        Id = id,
        UserName = "expert",
        Name = "E",
        Surname = "U",
        Email = "e@t",
        CityName = "Istanbul",
        Gender = "M",
        AuthType = "Local",
        InstitutionId = 1
    };

    private static UserDetailDto OfficialUser(int id = 200) => new()
    {
        Id = id,
        UserName = "official",
        Name = "O",
        Surname = "U",
        Email = "o@t",
        CityName = "Izmir",
        Gender = "F",
        AuthType = "Local",
        InstitutionId = 2
    };

    private static UserDetailDto BasicUser(int id = 300) => new()
    {
        Id = id,
        UserName = "user",
        Name = "U",
        Surname = "U",
        Email = "u@t",
        CityName = "Bursa",
        Gender = "M",
        AuthType = "Local",
        InstitutionId = 3
    };

    private static ProblemDetailDto SampleProblem(int id, bool resolved = false, int institutionId = 1) => new()
    {
        Id = id,
        SenderId = 1,
        Title = "T",
        Description = "D",
        SenderUsername = "s",
        CityName = "Ankara",
        IsResolved = resolved,
        InstitutionId = institutionId
    };

    private static (Mock<IUserService> u, Mock<IProblemService> p, Mock<ISolutionService> sol, RuleContextEnricher e) MakeHarness()
    {
        var u = new Mock<IUserService>();
        var p = new Mock<IProblemService>();
        var sol = new Mock<ISolutionService>();
        var services = new ServiceCollection();
        services.AddSingleton<IUserService>(u.Object);
        services.AddSingleton<IProblemService>(p.Object);
        services.AddSingleton<ISolutionService>(sol.Object);
        var sp = services.BuildServiceProvider();
        var e = new RuleContextEnricher(sp, NullLogger<RuleContextEnricher>.Instance);
        return (u, p, sol, e);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // (1) Anonim user — Metadata["AttemptedUserName"] üzerinden lookup
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AttemptedUserName_Lookup_Sets_SystemUserId_And_InstitutionId()
    {
        var (u, p, _, e) = MakeHarness();
        var admin = AdminUser(id: 9012, institutionId: 1);
        u.Setup(x => x.GetByUserName("admin123")).Returns(new User { Id = 9012, InstitutionId = 1 });
        u.Setup(x => x.GetById(9012)).Returns(new SuccessDataResult<UserDetailDto?>(admin));

        var ctx = new RuleContext
        {
            Metadata = new() { ["AttemptedUserName"] = "admin123" }
        };

        var enriched = await e.EnrichAsync(ctx);

        enriched.SystemUserId.Should().Be(9012);
        enriched.InstitutionId.Should().Be(1);
        enriched.UserRole.Should().Be("User"); // role flags removed; capability system handles roles
    }

    [Fact]
    public async Task AttemptedUserName_Unknown_User_Leaves_Context_Untouched()
    {
        var (u, p, _, e) = MakeHarness();
        u.Setup(x => x.GetByUserName(It.IsAny<string>())).Returns((User?)null!);

        var ctx = new RuleContext
        {
            Metadata = new() { ["AttemptedUserName"] = "ghost" }
        };

        var enriched = await e.EnrichAsync(ctx);

        enriched.SystemUserId.Should().Be(0);
        enriched.InstitutionId.Should().BeNull();
        enriched.UserRole.Should().Be("User"); // default
    }

    [Fact]
    public async Task AttemptedUserName_Lookup_Does_Not_Override_Explicit_SystemUserId()
    {
        var (u, p, _, e) = MakeHarness();
        // Already-set SystemUserId; metadata fallback çalışmamalı
        u.Setup(x => x.GetById(42)).Returns(new SuccessDataResult<UserDetailDto?>(AdminUser(42, 5)));

        var ctx = new RuleContext
        {
            SystemUserId = 42,
            Metadata = new() { ["AttemptedUserName"] = "should-not-be-used" }
        };

        var enriched = await e.EnrichAsync(ctx);

        enriched.SystemUserId.Should().Be(42);
        enriched.InstitutionId.Should().Be(5);
        u.Verify(x => x.GetByUserName(It.IsAny<string>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // (2) User enrichment — UserRole + UserSnapshot + InstitutionId
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SystemUserId_Pulls_UserSnapshot_And_Fills_Default_UserRole()
    {
        var (u, p, _, e) = MakeHarness();
        u.Setup(x => x.GetById(9012)).Returns(new SuccessDataResult<UserDetailDto?>(AdminUser(9012, 1)));

        var enriched = await e.EnrichAsync(new RuleContext { SystemUserId = 9012 });

        enriched.UserSnapshot.Should().NotBeNull();
        enriched.UserSnapshot!.UserName.Should().Be("admin123");
        enriched.UserRole.Should().Be("User"); // IsAdmin/IsExpert/IsOfficial removed; capability system replaced role flags
        enriched.InstitutionId.Should().Be(1);
    }

    [Theory]
    [InlineData("Expert")]
    [InlineData("Official")]
    [InlineData("User")]
    public async Task UserRole_Defaults_To_User_For_Any_Former_Role_Type(string userType)
    {
        // IsAdmin/IsExpert/IsOfficial flags were removed during capability migration.
        // DeriveRole always returns "User"; capabilities are checked separately via ICapabilityResolver.
        var (u, p, _, e) = MakeHarness();
        UserDetailDto dto = userType switch
        {
            "Expert"   => ExpertUser(),
            "Official" => OfficialUser(),
            _          => BasicUser()
        };
        u.Setup(x => x.GetById(dto.Id)).Returns(new SuccessDataResult<UserDetailDto?>(dto));

        var enriched = await e.EnrichAsync(new RuleContext { SystemUserId = dto.Id });

        enriched.UserRole.Should().Be("User");
    }

    [Fact]
    public async Task User_Lookup_Does_Not_Override_Explicit_InstitutionId()
    {
        var (u, p, _, e) = MakeHarness();
        u.Setup(x => x.GetById(1)).Returns(new SuccessDataResult<UserDetailDto?>(AdminUser(1, institutionId: 99)));

        var ctx = new RuleContext { SystemUserId = 1, InstitutionId = 7 };
        var enriched = await e.EnrichAsync(ctx);

        enriched.InstitutionId.Should().Be(7, "publisher tarafı InstitutionId set ettiyse enricher override etmemeli");
    }

    [Fact]
    public async Task User_Lookup_Failure_Leaves_Context_With_Defaults()
    {
        var (u, p, _, e) = MakeHarness();
        u.Setup(x => x.GetById(It.IsAny<int>()))
         .Returns(new ErrorDataResult<UserDetailDto?>(null, "yok"));

        var ctx = new RuleContext { SystemUserId = 999 };
        var enriched = await e.EnrichAsync(ctx);

        enriched.UserSnapshot.Should().BeNull();
        enriched.UserRole.Should().Be("User");
        enriched.InstitutionId.Should().BeNull();
    }

    [Fact]
    public async Task User_Lookup_Exception_Is_Swallowed()
    {
        var (u, p, _, e) = MakeHarness();
        u.Setup(x => x.GetById(It.IsAny<int>())).Throws(new InvalidOperationException("DB down"));

        var ctx = new RuleContext { SystemUserId = 1 };
        var act = async () => await e.EnrichAsync(ctx);
        await act.Should().NotThrowAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // (3) Problem enrichment — ProblemSnapshot + ProblemStatus
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProblemId_Pulls_ProblemSnapshot_And_Status_Open()
    {
        var (u, p, _, e) = MakeHarness();
        p.Setup(x => x.GetById(42)).Returns(new SuccessDataResult<ProblemDetailDto>(SampleProblem(42, resolved: false)));

        var enriched = await e.EnrichAsync(new RuleContext { ProblemId = 42 });

        enriched.ProblemSnapshot.Should().NotBeNull();
        enriched.ProblemSnapshot!.Id.Should().Be(42);
        enriched.ProblemStatus.Should().Be("Open");
    }

    [Fact]
    public async Task ProblemId_Pulls_ProblemSnapshot_And_Status_Resolved()
    {
        var (u, p, _, e) = MakeHarness();
        p.Setup(x => x.GetById(42)).Returns(new SuccessDataResult<ProblemDetailDto>(SampleProblem(42, resolved: true)));

        var enriched = await e.EnrichAsync(new RuleContext { ProblemId = 42 });

        enriched.ProblemStatus.Should().Be("Resolved");
    }

    [Fact]
    public async Task ProblemId_Lookup_Does_Not_Override_Explicit_ProblemStatus()
    {
        var (u, p, _, e) = MakeHarness();
        p.Setup(x => x.GetById(1)).Returns(new SuccessDataResult<ProblemDetailDto>(SampleProblem(1, resolved: false)));

        var enriched = await e.EnrichAsync(new RuleContext { ProblemId = 1, ProblemStatus = "InReview" });
        enriched.ProblemStatus.Should().Be("InReview");
    }

    [Fact]
    public async Task ProblemId_Fills_InstitutionId_If_Empty()
    {
        var (u, p, _, e) = MakeHarness();
        p.Setup(x => x.GetById(1)).Returns(new SuccessDataResult<ProblemDetailDto>(SampleProblem(1, institutionId: 77)));

        var enriched = await e.EnrichAsync(new RuleContext { ProblemId = 1 });
        enriched.InstitutionId.Should().Be(77);
    }

    [Fact]
    public async Task No_ProblemId_Skips_Problem_Lookup_Entirely()
    {
        var (u, p, _, e) = MakeHarness();
        await e.EnrichAsync(new RuleContext());
        p.Verify(x => x.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Null_Context_Returns_Empty_Context()
    {
        var (u, p, _, e) = MakeHarness();
        var enriched = await e.EnrichAsync(null!);
        enriched.Should().NotBeNull();
        enriched.SystemUserId.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // (4) Solution enrichment — İ8
    // ──────────────────────────────────────────────────────────────────────────

    private static Entities.Concrete.Solution SampleSolution(int id, int institutionId = 1, int approval = 1) => new()
    {
        Id                   = id,
        SenderId             = 5,
        ProblemId            = 10,
        Title                = "T",
        Description          = "D",
        IsHighlighted        = false,
        IsReported           = false,
        IsDeleted            = false,
        SendDate             = DateTime.UtcNow,
        ExpertApprovalStatus = approval,
        InstitutionId        = institutionId,
    };

    [Fact]
    public async Task I8_SolutionId_Pulls_SolutionSnapshot()
    {
        var (u, p, sol, e) = MakeHarness();
        sol.Setup(x => x.GetById(7)).Returns(new SuccessDataResult<Entities.Concrete.Solution?>(SampleSolution(7)));

        var enriched = await e.EnrichAsync(new RuleContext { SolutionId = 7 });

        enriched.SolutionSnapshot.Should().NotBeNull();
        enriched.SolutionSnapshot!.Id.Should().Be(7);
        enriched.SolutionSnapshot.ProblemId.Should().Be(10);
        enriched.SolutionSnapshot.ExpertApprovalStatus.Should().Be(1);
    }

    [Fact]
    public async Task I8_SolutionId_Fills_InstitutionId_If_Empty()
    {
        var (u, p, sol, e) = MakeHarness();
        sol.Setup(x => x.GetById(1)).Returns(new SuccessDataResult<Entities.Concrete.Solution?>(SampleSolution(1, institutionId: 88)));

        var enriched = await e.EnrichAsync(new RuleContext { SolutionId = 1 });
        enriched.InstitutionId.Should().Be(88);
    }

    [Fact]
    public async Task I8_No_SolutionId_Skips_Solution_Lookup()
    {
        var (u, p, sol, e) = MakeHarness();
        await e.EnrichAsync(new RuleContext());
        sol.Verify(x => x.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task I8_Solution_Lookup_Exception_Swallowed()
    {
        var (u, p, sol, e) = MakeHarness();
        sol.Setup(x => x.GetById(It.IsAny<int>())).Throws(new InvalidOperationException("DB down"));

        var act = async () => await e.EnrichAsync(new RuleContext { SolutionId = 1 });
        await act.Should().NotThrowAsync();
    }
}
