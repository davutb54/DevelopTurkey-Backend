using Core.Entities;

namespace Entities.Concrete;

public class FeatureDefinition : IEntity
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Key { get; set; } // Örn: "Auth_Google"
    public string DisplayName { get; set; } // Örn: "Google ile Giriş"
    public string InputType { get; set; } // "Boolean", "Text", "Number", "Select"
    public string DefaultValue { get; set; } // Örn: "true"
    public string? Description { get; set; }      // Açıklama metni
    public string? OptionsJson { get; set; }       // Select tipi için seçenekler: ["Seçenek1","Seçenek2"]
    public bool IsSystemLevel { get; set; } = false; // true ise sadece SuperAdmin değiştirebilir
    public int OrderIndex { get; set; } = 0;       // Grup içi sıralama
}
