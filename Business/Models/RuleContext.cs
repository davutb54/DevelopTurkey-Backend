using System;
using Entities.DTOs;
using Entities.DTOs.User;

namespace Business.Models;

/// <summary>
/// Roslyn C# Script motoru tarafından çalıştırılan kurallara aktarılan bağlam nesnesi.
/// Kural kodu bu nesneye "context" adıyla erişebilir.
/// </summary>
[Serializable]
public class RuleContext
{
    /// <summary>Kuralı tetikleyen kullanıcının Id'si.</summary>
    public int SystemUserId { get; set; }

    /// <summary>Kuralı tetikleyen kullanıcının rolü (örn. "SuperAdmin", "Admin", "User").</summary>
    public string UserRole { get; set; } = "User";

    /// <summary>Kuralı tetikleyen olayın adı (örn. "ProblemCreated").</summary>
    public string TriggerEventName { get; set; } = string.Empty;

    /// <summary>Kuralın ilişkili olduğu problem Id'si (varsa).</summary>
    public int? ProblemId { get; set; }

    /// <summary>Aksiyonun hedef alacağı kullanıcı Id'si (varsa).</summary>
    public int? TargetUserId { get; set; }

    /// <summary>Kuralın ilişkili olduğu kurum Id'si (varsa).</summary>
    public int? InstitutionId { get; set; }

    /// <summary>Kuralın ilişkili olduğu çözüm Id'si (varsa).</summary>
    public int? SolutionId { get; set; }

    /// <summary>Kuralın ilişkili olduğu yorum Id'si (varsa).</summary>
    public int? CommentId { get; set; }

    /// <summary>Kullanıcı puanı (varsa).</summary>
    public int? UserScore { get; set; }

    /// <summary>Kullanıcının problem sayısı (varsa).</summary>
    public int? UserProblemCount { get; set; }

    /// <summary>Problemin durumu (varsa).</summary>
    public string? ProblemStatus { get; set; }

    /// <summary>Problemin zorluk seviyesi (varsa).</summary>
    public string? ProblemDifficulty { get; set; }

    /// <summary>Önceki değer (feature geçişleri veya güncellemeler için).</summary>
    public string OldValue { get; set; } = string.Empty;

    /// <summary>Yeni değer (feature geçişleri veya güncellemeler için).</summary>
    public string NewValue { get; set; } = string.Empty;

    /// <summary>Kullanıcı anlık görüntüsü (DTO formunda).</summary>
    public UserDetailDto? UserSnapshot { get; set; }

    /// <summary>Problem anlık görüntüsü (DTO formunda).</summary>
    public ProblemDetailDto? ProblemSnapshot { get; set; }

    /// <summary>Çözüm anlık görüntüsü (DTO formunda).</summary>
    public SolutionDetailDto? SolutionSnapshot { get; set; }

    /// <summary>Kural çalışma zamanında kullanılabilecek ek meta veriler (key-value).</summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Kuralın çalıştırıldığı UTC zaman damgası.</summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
