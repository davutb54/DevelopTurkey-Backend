using Microsoft.AspNetCore.Http;

namespace Entities.DTOs;

public class SolutionUpdateDto
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int SenderId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string? ImageUrls { get; set; } // Mevcut resimlerin virgülle ayrılmış listesi
    public List<IFormFile>? Images { get; set; } // Yeni eklenecek resimler
    public DateTime SendDate { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsReported { get; set; }
    public bool IsDeleted { get; set; }
    public int ExpertApprovalStatus { get; set; }
    public int InstitutionId { get; set; }
}
