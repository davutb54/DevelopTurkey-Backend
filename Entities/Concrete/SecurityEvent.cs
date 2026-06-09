using Core.Entities;

namespace Entities.Concrete;

public class SecurityEvent : IEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>failed_login | rate_limited | capability_denied | enumeration_detected</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>low | medium | high | critical</summary>
    public string Severity { get; set; } = "low";

    public string? Path { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? InstitutionId { get; set; }
}
