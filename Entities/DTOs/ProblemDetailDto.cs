namespace Entities.DTOs;

public class ProblemDetailDto
{
    public bool SenderIsOfficial { get; set; }

    public int Id { get; set; }
    public string? PublicId { get; set; }
	public int SenderId { get; set; }
    public List<TopicDto> Topics { get; set; }
    public string SenderUsername { get; set; }
	public bool SenderIsExpert { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public int? CityCode { get; set; }
    public int? CustomHierarchyId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
	public string CityName { get; set; }
    public string? CustomHierarchyName { get; set; }
	public bool IsHighlighted { get; set; }
	public bool IsReported { get; set; }
	public bool IsDeleted { get; set; }
	public DateTime SendDate { get; set; }
    public List<string>? ImageUrls { get; set; }
    public List<string>? VideoUrls { get; set; }
    public int ViewCount { get; set; }
    public int SolutionCount { get; set; }
    public bool IsResolvedByExpert { get; set; }
    public bool IsResolved { get; set; }
    public string? SenderImageUrl { get; set; }
    public int InstitutionId { get; set; }
    public int UpvoteCount { get; set; }
    public int FollowerCount { get; set; }
    public List<UserTitleDto> SenderTitles { get; set; } = new();
    public List<OfficialResponseDto> OfficialResponses { get; set; } = new();

    // Görünürlük seviyeleri — frontend'in "kapalı" bölümleri tamamen gizlemesi için
    public string ViewersVisibility { get; set; } = "admin_only";
    public string UpvotersVisibility { get; set; } = "admin_and_owner";
    public string ParticipantsVisibility { get; set; } = "public";
    public string SolutionVotersVisibility { get; set; } = "admin_and_owner";

    // Kapatma & gizleme
    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public string? CloseReason { get; set; }
    public bool IsHidden { get; set; } = false;
}