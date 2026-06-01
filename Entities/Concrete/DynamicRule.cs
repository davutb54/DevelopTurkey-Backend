using Core.Entities;

using System;

namespace Entities.Concrete;

public class DynamicRule : IEntity
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string Name { get; set; } // Örn: "Bilgi İşlem Otomatik Atama"
    public string TriggerEvent { get; set; } // Örn: "OnProblemCreated"
    public string FlowJson { get; set; }
    public int FlowJsonSchemaVersion { get; set; } = 1;
    public int Version { get; set; } = 1;
    public int Priority { get; set; } = 100;
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
