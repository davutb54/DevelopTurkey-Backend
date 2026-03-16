using Business.Abstract;
using Business.Constants;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace Business.Concrete;

public class TopicManager : ITopicService
{
    private readonly ITopicDal _topicDal;
    private readonly ILogService _logService;
    private readonly IClientContext _clientContext;
    
    public TopicManager(ITopicDal topicDal, ILogService logService, IClientContext clientContext)
    {
        _topicDal = topicDal;
        _logService = logService;
        _clientContext = clientContext;
    }

    public IDataResult<Topic?> GetById(int id)
    {
        return new SuccessDataResult<Topic?>(_topicDal.Get(topic => topic.Id == id));
    }

    public IDataResult<List<Topic>> GetAll()
    {
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var topics = _topicDal.GetAll(t => t.Status == true && (t.InstitutionId == institutionId || t.InstitutionId == 1));
        return new SuccessDataResult<List<Topic>>(topics);
    }

    public IDataResult<List<Topic>> GetAllForAdmin()
    {
        return new SuccessDataResult<List<Topic>>(_topicDal.GetAll());
    }

    public IResult Add(Topic topic)
    {
        _topicDal.Add(topic);

        _logService.LogInfo("Content", "Add", $"Yeni konu eklendi: {topic.Name}");
        return new SuccessResult(Messages.TopicAdded);
    }

    public IResult Update(Topic topic)
    {
        _topicDal.Update(topic);
        _logService.LogInfo("Content", "Update", $"Konu güncellendi: {topic.Name}");
        return new SuccessResult(Messages.TopicUpdated);
    }

    public IResult Delete(Topic topic)
    {
        _topicDal.Delete(topic);
        _logService.LogWarning("Content", "Delete", $"Konu silindi: {topic.Name}");
        return new SuccessResult(Messages.TopicDeleted);
    }
}