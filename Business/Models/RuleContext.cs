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
    // ══════════════════════════════════════════════════════════════════════════
    // İŞLEMİ YAPAN KULLANICI (System User)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Kuralı tetikleyen kullanıcının Id'si.</summary>
    public int SystemUserId { get; set; }

    /// <summary>Kuralı tetikleyen kullanıcının rolü (örn. "SuperAdmin", "Admin", "User").</summary>
    public string UserRole { get; set; } = "User";

    /// <summary>Tetikleyen kullanıcının puanı.</summary>
    public int? UserScore { get; set; }

    /// <summary>Tetikleyen kullanıcının toplam problem sayısı.</summary>
    public int? UserProblemCount { get; set; }

    /// <summary>Tetikleyen kullanıcının kurum Id'si.</summary>
    public int? InstitutionId { get; set; }

    /// <summary>Tetikleyen kullanıcının yasaklı olup olmadığı.</summary>
    public bool? UserIsBanned { get; set; }

    /// <summary>Tetikleyen kullanıcının e-posta doğrulama durumu.</summary>
    public bool? UserIsEmailVerified { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // HEDEF KULLANICI (Target User) — İşlemden etkilenen kişi
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Aksiyonun hedef alacağı kullanıcı Id'si (varsa).</summary>
    public int? TargetUserId { get; set; }

    /// <summary>Hedef kullanıcının rolü (Admin, Expert, Official, User).</summary>
    public string? TargetUserRole { get; set; }

    /// <summary>Hedef kullanıcının puanı.</summary>
    public int? TargetUserScore { get; set; }

    /// <summary>Hedef kullanıcının kurum Id'si.</summary>
    public int? TargetUserInstitutionId { get; set; }

    /// <summary>Hedef kullanıcının yasaklı olup olmadığı.</summary>
    public bool? TargetUserIsBanned { get; set; }

    /// <summary>Hedef kullanıcının e-posta doğrulama durumu.</summary>
    public bool? TargetUserIsEmailVerified { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // PROBLEM ALANLARI
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>İlişkili problem Id'si (varsa).</summary>
    public int? ProblemId { get; set; }

    /// <summary>Problemin sahibinin (yazarının) kullanıcı Id'si.</summary>
    public int? ProblemOwnerId { get; set; }

    /// <summary>Problemin durumu (Open, Resolved).</summary>
    public string? ProblemStatus { get; set; }

    /// <summary>Problemin zorluk seviyesi.</summary>
    public string? ProblemDifficulty { get; set; }

    /// <summary>Problemin kurum Id'si.</summary>
    public int? ProblemInstitutionId { get; set; }

    /// <summary>Problemin görüntülenme sayısı.</summary>
    public int? ProblemViewCount { get; set; }

    /// <summary>Problemin çözüm sayısı.</summary>
    public int? ProblemSolutionCount { get; set; }

    /// <summary>Problemin oy sayısı.</summary>
    public int? ProblemUpvoteCount { get; set; }

    /// <summary>Problemin takipçi sayısı.</summary>
    public int? ProblemFollowerCount { get; set; }

    /// <summary>Problemin öne çıkarılmış olup olmadığı.</summary>
    public bool? ProblemIsHighlighted { get; set; }

    /// <summary>Problemin şikayet edilmiş olup olmadığı.</summary>
    public bool? ProblemIsReported { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // ÇÖZÜM ALANLARI
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>İlişkili çözüm Id'si (varsa).</summary>
    public int? SolutionId { get; set; }

    /// <summary>Çözümün sahibinin (yazarının) kullanıcı Id'si.</summary>
    public int? SolutionOwnerId { get; set; }

    /// <summary>Çözümün kurum Id'si.</summary>
    public int? SolutionInstitutionId { get; set; }

    /// <summary>Çözümün oy sayısı.</summary>
    public int? SolutionVoteCount { get; set; }

    /// <summary>Çözümün onay durumu (0=Bekliyor, 1=Onaylandı, 2=Reddedildi).</summary>
    public int? SolutionApprovalStatus { get; set; }

    /// <summary>Çözümün öne çıkarılmış olup olmadığı.</summary>
    public bool? SolutionIsHighlighted { get; set; }

    /// <summary>Çözümün şikayet edilmiş olup olmadığı.</summary>
    public bool? SolutionIsReported { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // YORUM / KONU / DİĞER ID'LER
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>İlişkili yorum Id'si (varsa).</summary>
    public int? CommentId { get; set; }

    // ══════════════════════════════════════════════════════════════════════════
    // OLAY META VERİLERİ
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Kuralı tetikleyen olayın adı (örn. "problem.created").</summary>
    public string TriggerEventName { get; set; } = string.Empty;

    /// <summary>Önceki değer (güncellemeler için).</summary>
    public string OldValue { get; set; } = string.Empty;

    /// <summary>Yeni değer (güncellemeler için).</summary>
    public string NewValue { get; set; } = string.Empty;

    // ══════════════════════════════════════════════════════════════════════════
    // SNAPSHOT'LAR (Roslyn C# Script için tam erişim)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Kullanıcı anlık görüntüsü (DTO formunda).</summary>
    public UserDetailDto? UserSnapshot { get; set; }

    /// <summary>Problem anlık görüntüsü (DTO formunda).</summary>
    public ProblemDetailDto? ProblemSnapshot { get; set; }

    /// <summary>Çözüm anlık görüntüsü (DTO formunda).</summary>
    public SolutionDetailDto? SolutionSnapshot { get; set; }

    /// <summary>Hedef kullanıcının anlık görüntüsü (DTO formunda).</summary>
    public UserDetailDto? TargetUserSnapshot { get; set; }

    /// <summary>Kural çalışma zamanında kullanılabilecek ek meta veriler (key-value).</summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Kuralın çalıştırıldığı UTC zaman damgası.</summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

