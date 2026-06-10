using System.ComponentModel.DataAnnotations;

namespace Core.Entities.Concrete;

public class User : IEntity
{
    public int Id { get; set; }
    [MaxLength(100)]
    public string UserName { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(100)]
    public string Surname { get; set; }
    [MaxLength(320)]
    public string Email { get; set; }
    public string? ProfileImageUrl { get; set; }

    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public string AuthType { get; set; } = "Local";

    public int CityCode { get; set; }
    public int Gender { get; set; }
    public bool EmailNotificationPermission { get; set; } = true;
    public bool MentionNotificationEnabled { get; set; } = true;

    // Gizlilik Ayarları
    public bool IsProfilePublic { get; set; } = true;
    public bool ShowSolutions { get; set; } = true;
    public bool ShowProblems { get; set; } = true;

    public bool IsReported { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public bool IsBanned { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;
    public DateTime RegisterDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    public DateTime? LastUsernameChangeDate { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public int LockoutCount { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    public int InstitutionId { get; set; }
    public int? CustomHierarchyId { get; set; }
}