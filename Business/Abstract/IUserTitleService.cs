using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IUserTitleService
{
    IDataResult<List<UserTitleDto>> GetByUser(int userId);
    IResult Assign(UserTitleAddDto dto);
    IResult Remove(int id);
}
