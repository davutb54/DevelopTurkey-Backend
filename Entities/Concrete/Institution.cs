using Core.Entities;

namespace Entities.Concrete
{
    public class Institution : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Domain { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public bool Status { get; set; }
    }
}