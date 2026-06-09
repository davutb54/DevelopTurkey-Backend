using Core.Entities;

namespace Entities.Concrete;

public class Problem : IEntity
{
	public int Id { get; set; }
	public int SenderId { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public int CityCode { get; set; }
	public string? Address { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
	public bool IsHighlighted { get; set; } = false;
	public bool IsReported { get; set; } = false;
	public bool IsDeleted { get; set; } = false;
	public DateTime SendDate { get; set; }
	public DateTime? DeleteDate { get; set; }
    public string? ImageUrls { get; set; }
    public int ViewCount { get; set; }
    public bool IsResolved { get; set; }

    public int InstitutionId { get; set; }
    public int? CustomHierarchyId { get; set; }

    public bool IsClosed { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public string? CloseReason { get; set; }
    public bool IsHidden { get; set; } = false;
}