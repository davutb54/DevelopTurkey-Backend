using Core.Entities;

namespace Entities.Concrete;

public class ProblemTopic : IEntity
{
    public int ProblemId { get; set; }
    public int TopicId { get; set; }
}