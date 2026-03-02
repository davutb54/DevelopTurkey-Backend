using Core.Entities;

namespace Entities.Concrete;

public class Feedback : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime SendDate { get; set; }
}