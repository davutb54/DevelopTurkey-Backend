using Core.Entities;

namespace Entities.Concrete;

public class Topic : IEntity
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string ImageName { get; set; }
}