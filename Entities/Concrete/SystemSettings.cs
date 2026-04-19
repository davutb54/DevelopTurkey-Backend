using Core.Entities;

namespace Entities.Concrete;

public class SystemSettings : IEntity
{
    public int Id { get; set; }
    public bool IsMaintenanceMode { get; set; }
    public bool DisableNewRegistrations { get; set; }
    public string? MaintenanceMessage { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}
