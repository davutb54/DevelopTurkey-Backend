namespace Entities.DTOs;

public class SaveWorkflowDto
{
    /// <summary>Varsa mevcut kuralın ID'si. Dolu ise güncelleme, boşsa yeni kayıt yapılır.</summary>
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string FlowJson { get; set; } = string.Empty;
    public int FlowJsonSchemaVersion { get; set; } = 1;
    public int Priority { get; set; } = 100;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
