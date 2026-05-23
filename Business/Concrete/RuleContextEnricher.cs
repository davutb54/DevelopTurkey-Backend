using Business.Abstract;
using Business.Models;
using Entities.DTOs.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// IRuleContextEnricher default implementation.
///
/// DI tasarımı: UserService → InstitutionService → WorkflowEventBus zinciri nedeniyle
/// Enricher constructor'ında IUserService/IProblemService inject EDİLEMEZ
/// (circular dependency). Bunun yerine IServiceProvider üzerinden runtime'da
/// scope içinde resolve edilir.
///
/// Enrichment kuralları:
///   1) SystemUserId == 0 fakat Metadata["AttemptedUserName"] varsa (örn. login_failed),
///      UserService.GetByUserName ile kullanıcıyı bul, varsa SystemUserId/InstitutionId
///      set et.
///   2) SystemUserId > 0 ise UserService.GetById ile UserSnapshot/UserRole/InstitutionId
///      doldurulur.
///   3) ProblemId.HasValue ise ProblemService.GetById ile ProblemSnapshot/ProblemStatus
///      doldurulur.
/// Her aşama izole try/catch ile korunur: bir lookup hatası event'i bozmaz.
/// </summary>
public class RuleContextEnricher : IRuleContextEnricher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RuleContextEnricher> _logger;

    public RuleContextEnricher(
        IServiceProvider serviceProvider,
        ILogger<RuleContextEnricher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task<RuleContext> EnrichAsync(RuleContext context)
    {
        if (context == null) return Task.FromResult(new RuleContext());

        TryResolveUserFromMetadata(context);
        TryEnrichUser(context);
        TryEnrichProblem(context);
        TryEnrichSolution(context);

        return Task.FromResult(context);
    }

    // ── (1) Anonim user (auth.login_failed gibi) ─────────────────────────────
    private void TryResolveUserFromMetadata(RuleContext context)
    {
        if (context.SystemUserId != 0) return;
        if (context.Metadata == null) return;
        if (!context.Metadata.TryGetValue("AttemptedUserName", out var raw)) return;

        var userName = raw as string;
        if (string.IsNullOrWhiteSpace(userName)) return;

        try
        {
            var userService = _serviceProvider.GetService<IUserService>();
            if (userService == null) return;
            var user = userService.GetByUserName(userName);
            if (user != null)
            {
                context.SystemUserId = user.Id;
                if (!context.InstitutionId.HasValue) context.InstitutionId = user.InstitutionId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[RuleContextEnricher] AttemptedUserName lookup başarısız: {UserName}", userName);
        }
    }

    // ── (2) UserSnapshot + UserRole + InstitutionId ─────────────────────────────
    private void TryEnrichUser(RuleContext context)
    {
        if (context.SystemUserId > 0 && context.UserSnapshot == null)
        {
            try
            {
                var userService = _serviceProvider.GetService<IUserService>();
                if (userService != null)
                {
                    var result = userService.GetById(context.SystemUserId);
                    if (result?.Success == true && result.Data != null)
                    {
                        context.UserSnapshot = result.Data;
                        if (!context.InstitutionId.HasValue)
                            context.InstitutionId = result.Data.InstitutionId;

                        if (string.IsNullOrEmpty(context.UserRole) || context.UserRole == "User")
                            context.UserRole = DeriveRole(result.Data);

                        context.UserIsBanned = result.Data.IsBanned;
                        context.UserIsEmailVerified = result.Data.IsEmailVerified;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[RuleContextEnricher] User enrichment başarısız: UserId={UserId}",
                    context.SystemUserId);
            }
        }

        // TargetUser'ı da zenginleştir (Aksiyon hedefi)
        if (context.TargetUserId.HasValue && context.TargetUserId > 0 && context.TargetUserSnapshot == null)
        {
            try
            {
                var userService = _serviceProvider.GetService<IUserService>();
                if (userService != null)
                {
                    var result = userService.GetById(context.TargetUserId.Value);
                    if (result?.Success == true && result.Data != null)
                    {
                        context.TargetUserSnapshot = result.Data;
                        context.TargetUserRole = DeriveRole(result.Data);
                        context.TargetUserInstitutionId = result.Data.InstitutionId;
                        context.TargetUserIsAdmin = result.Data.IsAdmin;
                        context.TargetUserIsExpert = result.Data.IsExpert;
                        context.TargetUserIsOfficial = result.Data.IsOfficial;
                        context.TargetUserIsBanned = result.Data.IsBanned;
                        context.TargetUserIsEmailVerified = result.Data.IsEmailVerified;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[RuleContextEnricher] TargetUser enrichment başarısız: TargetUserId={TargetUserId}",
                    context.TargetUserId);
            }
        }
    }

    // ── (3) ProblemSnapshot + ProblemStatus ─────────────────────────────────────
    private void TryEnrichProblem(RuleContext context)
    {
        if (!context.ProblemId.HasValue) return;
        if (context.ProblemSnapshot != null) return;

        try
        {
            var problemService = _serviceProvider.GetService<IProblemService>();
            if (problemService == null) return;
            var result = problemService.GetById(context.ProblemId.Value);
            if (result?.Success == true && result.Data != null)
            {
                context.ProblemSnapshot = result.Data;

                context.ProblemOwnerId = result.Data.SenderId;
                context.ProblemStatus = result.Data.IsResolved ? "Resolved" : "Open";
                context.ProblemInstitutionId = result.Data.InstitutionId;
                context.ProblemViewCount = result.Data.ViewCount;
                context.ProblemSolutionCount = result.Data.SolutionCount;
                context.ProblemUpvoteCount = result.Data.UpvoteCount;
                context.ProblemFollowerCount = result.Data.FollowerCount;
                context.ProblemIsHighlighted = result.Data.IsHighlighted;
                context.ProblemIsReported = result.Data.IsReported;
                // ProblemDifficulty şu an ProblemDetailDto'da yok, sabitlenmiyor.

                if (!context.InstitutionId.HasValue)
                    context.InstitutionId = result.Data.InstitutionId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[RuleContextEnricher] Problem enrichment başarısız: ProblemId={ProblemId}",
                context.ProblemId);
        }
    }

    // ── (4) SolutionSnapshot (İ8) ──────────────────────────────────────────────
    private void TryEnrichSolution(RuleContext context)
    {
        if (!context.SolutionId.HasValue) return;
        if (context.SolutionSnapshot != null) return;

        try
        {
            var solutionService = _serviceProvider.GetService<ISolutionService>();
            if (solutionService == null) return;
            var result = solutionService.GetById(context.SolutionId.Value);
            if (result?.Success == true && result.Data != null)
            {
                var s = result.Data;
                context.SolutionSnapshot = new Entities.DTOs.SolutionDetailDto
                {
                    Id                   = s.Id,
                    SenderId             = s.SenderId,
                    ProblemId            = s.ProblemId,
                    Title                = s.Title,
                    Description          = s.Description,
                    SenderUsername       = string.Empty, 
                    ProblemName          = string.Empty,
                    IsHighlighted        = s.IsHighlighted,
                    IsReported           = s.IsReported,
                    IsDeleted            = s.IsDeleted,
                    SendDate             = s.SendDate,
                    ExpertApprovalStatus = s.ExpertApprovalStatus,
                    InstitutionId        = s.InstitutionId,
                };

                context.SolutionOwnerId = s.SenderId;
                context.SolutionInstitutionId = s.InstitutionId;
                context.SolutionApprovalStatus = s.ExpertApprovalStatus;
                context.SolutionIsHighlighted = s.IsHighlighted;
                context.SolutionIsReported = s.IsReported;
                // Solution entity'sinde VoteCount yok, hesaplanması (veya DTO'dan gelmesi) gerekir.
                context.SolutionVoteCount = 0; 

                if (!context.InstitutionId.HasValue)
                    context.InstitutionId = s.InstitutionId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[RuleContextEnricher] Solution enrichment başarısız: SolutionId={SolutionId}",
                context.SolutionId);
        }
    }

    /// <summary>
    /// UserDetailDto'daki rol bayraklarından (IsAdmin/IsExpert/IsOfficial) öncelikli
    /// rolü türetir. Sistemde "SuperAdmin" kavramı `IsAdmin == true` olarak temsil
    /// edilir (bkz. RuleExecutionManager.cs:17).
    /// </summary>
    private static string DeriveRole(UserDetailDto user)
    {
        if (user.IsAdmin) return "Admin";
        if (user.IsExpert) return "Expert";
        if (user.IsOfficial) return "Official";
        return "User";
    }
}
