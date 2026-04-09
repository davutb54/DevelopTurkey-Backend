using Business.Abstract;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using System.Collections.Generic;

namespace Business.Concrete;

public class FeedbackManager : IFeedbackService
{
    private readonly IFeedbackDal _feedbackDal;
    private readonly ILogService _logService;
    private readonly IClientContext _clientContext;

    public FeedbackManager(IFeedbackDal feedbackDal, ILogService logService, IClientContext clientContext)
    {
        _feedbackDal = feedbackDal;
        _logService = logService;
        _clientContext = clientContext;
    }

    public IResult Add(Feedback feedback)
    {
        feedback.UserId = _clientContext.GetUserId() ?? 0;
        _feedbackDal.Add(feedback);
        _logService.LogInfo("Feedback","Add", $"Yeni Geribildirim eklendi: {feedback.Title}");
        return new SuccessResult("Geri bildirim başarıyla gönderildi.");
    }

    public IResult Update(Feedback feedback)
    {
        _feedbackDal.Update(feedback);
            _logService.LogInfo("Feedback","Update", $"Geri bildirim güncellendi: {feedback.Title}");
        return new SuccessResult("Geri bildirim güncellendi.");
    }

    public IResult Delete(int id)
    {
        var feedback = _feedbackDal.Get(f => f.Id == id);
        if (feedback == null)
        {
            return new ErrorResult("Silinecek geri bildirim bulunamadı.");
        }

        _feedbackDal.Delete(feedback);
        _logService.LogWarning("Feedback","Delete", $"Geri bildirim silindi: {feedback.Title}");
        return new SuccessResult("Geri bildirim silindi.");
    }

    public IDataResult<Feedback> GetById(int id)
    {
        var feedback = _feedbackDal.Get(f => f.Id == id);
        if (feedback == null)
        {
            return new ErrorDataResult<Feedback>(null, "Geri bildirim bulunamadı.");
        }

        return new SuccessDataResult<Feedback>(feedback);
    }

    public IDataResult<List<FeedbackDetailDto>> GetAllDetails()
    {
        var result = _feedbackDal.GetFeedbackDetails();
        return new SuccessDataResult<List<FeedbackDetailDto>>(result, "Geri bildirimler listelendi.");
    }

    public IDataResult<(List<FeedbackDetailDto> Items, int TotalCount)> GetAllDetailsPaged(FeedbackFilterDto filter)
    {
        var result = _feedbackDal.GetFeedbackDetailsPaged(filter);
        return new SuccessDataResult<(List<FeedbackDetailDto> Items, int TotalCount)>(result, "Geri bildirimler listelendi.");
    }
}