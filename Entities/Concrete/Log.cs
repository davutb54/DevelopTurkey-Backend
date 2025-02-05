using Core.Entities;

namespace Entities.Concrete;

public class Log:IEntity
{
	public int Id { get; set; }
	public string Message { get; set; }
	public DateTime CreationDate { get; set; }
	public string Type { get; set; }
}