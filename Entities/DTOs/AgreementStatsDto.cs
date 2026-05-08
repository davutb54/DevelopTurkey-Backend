namespace Entities.DTOs;

public class AgreementStatsDto
{
    public int AgreementId { get; set; }
    public int AcceptanceCount { get; set; }
    public double AcceptanceRate { get; set; }  // 0.0 – 100.0
}
