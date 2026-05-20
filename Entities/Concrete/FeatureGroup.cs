using Core.Entities;

namespace Entities.Concrete;

public class FeatureGroup : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } // Örn: "Kimlik ve Erişim"
    public int OrderIndex { get; set; }
}
