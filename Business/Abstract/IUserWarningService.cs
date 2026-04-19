using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IUserWarningService
{
    IDataResult<List<UserWarning>> GetByUserId(int userId);
    IDataResult<int> GetActiveWarningCount(int userId);
    IResult Issue(UserWarning warning);
    IResult Revoke(int warningId);
}
