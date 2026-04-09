using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using System.Collections.Generic;

namespace Business.Abstract;

public interface IFeedbackService
{
    IResult Add(Feedback feedback);

    IResult Update(Feedback feedback);

    IResult Delete(int id);

    IDataResult<Feedback> GetById(int id);
    IDataResult<List<FeedbackDetailDto>> GetAllDetails();
    IDataResult<(List<FeedbackDetailDto> Items, int TotalCount)> GetAllDetailsPaged(FeedbackFilterDto filter);
}