namespace Entities.DTOs;

// ── Conversation ──────────────────────────────────────────────────────────────

public class StartDirectConversationDto
{
    public int TargetUserId { get; set; }
}

public class StartGroupConversationDto
{
    public string Title { get; set; } = string.Empty;
    public List<int> ParticipantUserIds { get; set; } = new();
}

/// <summary>Destek talebi oluşturma — type=support, Status=pending.</summary>
public class StartSupportConversationDto
{
    /// <summary>general | official | admin</summary>
    public string Category { get; set; } = "general";
    public string? InitialMessage { get; set; }
}

public class UpdateConversationTitleDto
{
    public string Title { get; set; } = string.Empty;
}

public class ConversationSummaryDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public int? InstitutionId { get; set; }
    public string? Title { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ParticipantCount { get; set; }
    public int UnreadCount { get; set; }
    public MessageDto? LastMessage { get; set; }

    // ── Destek alanları ──
    public string Status { get; set; } = "active";
    public string? SupportCategory { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUsername { get; set; }
}

public class ConversationDetailDto : ConversationSummaryDto
{
    public List<ParticipantDto> Participants { get; set; } = new();
}

public class ParticipantDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? LastReadMessageId { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class AddParticipantDto
{
    public int UserId { get; set; }
}

// ── Message ───────────────────────────────────────────────────────────────────

public class SendMessageDto
{
    public string Body { get; set; } = string.Empty;
}

public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class MessagePageDto
{
    public List<MessageDto> Items { get; set; } = new();
    public bool HasMore { get; set; }
}

public class MarkReadDto
{
    public int LastReadMessageId { get; set; }
}
