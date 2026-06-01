using System.Reflection;
using System.Text.RegularExpressions;
using Business.Models;

namespace Business.Concrete.Actions.Helpers;

/// <summary>
/// Tüm action handler'lar tarafından paylaşılan parametre çözümleme yardımcıları.
/// Statik sınıf — durum içermez, DI gerekmez.
/// </summary>
public static partial class WorkflowParameterResolver
{
    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderPattern();

    // ── Temel çözümleme ────────────────────────────────────────────────────────

    /// <summary>
    /// Parametre değerindeki {Prefix.Property} veya {Property} placeholder'larını
    /// RuleContext'teki runtime değerleriyle değiştirir.
    /// </summary>
    public static string Resolve(string? template, RuleContext context)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        return PlaceholderPattern().Replace(template, match =>
        {
            var path = match.Groups[1].Value;
            return ResolvePath(path, context) ?? match.Value;
        });
    }

    private static string? ResolvePath(string path, RuleContext context)
    {
        var dotIndex = path.IndexOf('.');
        if (dotIndex < 0)
            return GetPropertyValue(context, path)?.ToString();

        var prefix   = path[..dotIndex].ToLowerInvariant();
        var property = path[(dotIndex + 1)..];

        return prefix switch
        {
            "user"     => GetPropertyValue(context.UserSnapshot,     property)?.ToString(),
            "problem"  => GetPropertyValue(context.ProblemSnapshot,  property)?.ToString(),
            "solution" => GetPropertyValue(context.SolutionSnapshot, property)?.ToString(),
            "context"  => GetPropertyValue(context, property)?.ToString(),
            _          => null
        };
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj is null) return null;
        return obj.GetType()
            .GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?.GetValue(obj);
    }

    // ── Hedef çözümleme ───────────────────────────────────────────────────────

    /// <summary>
    /// userTarget / customUserId parametrelerinden hedef kullanıcı ID'sini döner.
    /// </summary>
    public static int ResolveUserId(
        Dictionary<string, string> parameters,
        RuleContext context,
        string targetKey   = "userTarget",
        string customIdKey = "customUserId")
    {
        var userTarget = parameters.GetValueOrDefault(targetKey) ?? "context_user";
        return userTarget switch
        {
            "target_user" => context.TargetUserId ?? context.SystemUserId,
            "custom"      => int.TryParse(Resolve(parameters.GetValueOrDefault(customIdKey), context), out var cid)
                                 ? cid
                                 : context.SystemUserId,
            _             => context.SystemUserId
        };
    }

    /// <summary>problemTarget / customProblemId parametrelerinden problem ID döner.</summary>
    public static int? ResolveProblemId(Dictionary<string, string> parameters, RuleContext context)
    {
        var target = parameters.GetValueOrDefault("problemTarget") ?? "context_problem";
        if (target == "custom")
        {
            if (int.TryParse(Resolve(parameters.GetValueOrDefault("customProblemId"), context), out var cid))
                return cid;
            return null;
        }
        return context.ProblemId;
    }

    /// <summary>solutionTarget / customSolutionId parametrelerinden çözüm ID döner.</summary>
    public static int? ResolveSolutionId(Dictionary<string, string> parameters, RuleContext context)
    {
        var target = parameters.GetValueOrDefault("solutionTarget") ?? "context_solution";
        if (target == "custom")
        {
            if (int.TryParse(Resolve(parameters.GetValueOrDefault("customSolutionId"), context), out var cid))
                return cid;
            return null;
        }
        return context.SolutionId;
    }

    // ── Doğrulama yardımcıları ─────────────────────────────────────────────────

    /// <summary>Zorunlu parametre kontrolü — eksik/boşsa errors listesine ekler.</summary>
    public static void RequireParam(
        Dictionary<string, string> parameters,
        string key,
        string actionCode,
        IList<string> errors)
    {
        if (string.IsNullOrWhiteSpace(parameters.GetValueOrDefault(key)))
            errors.Add($"{actionCode}: '{key}' parametresi zorunludur.");
    }
}
