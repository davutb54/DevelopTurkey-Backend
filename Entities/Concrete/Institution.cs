using Core.Entities;

namespace Entities.Concrete
{
    public class Institution : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subtitle { get; set; } = "Özel Kurum Ağı";
        public string Domain { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        // CustomFieldsJson kalıyor
        public string CustomFieldsJson { get; set; } = "[]";
        public string? CustomHierarchyLabel { get; set; }
        public string? CustomHierarchyJson { get; set; }
        public bool Status { get; set; }
    }
}
