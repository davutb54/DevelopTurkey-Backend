using Core.Entities;

namespace Entities.Concrete;

public class AboutPageSection : IEntity
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; } // Markdown formatı
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
