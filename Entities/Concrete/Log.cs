using Core.Entities;

namespace Entities.Concrete;

public class Log : IEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? Port { get; set; }
    public string Category { get; set; }
    public string Action { get; set; }
    public string Level { get; set; } 
    public string Message { get; set; }
    public string? Details { get; set; } 
    public DateTime CreationDate { get; set; } = DateTime.Now;
}