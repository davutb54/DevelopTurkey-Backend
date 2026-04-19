using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface ISystemSettingsService
{
    IDataResult<SystemSettings> Get();
    IResult Update(SystemSettings settings, int adminUserId);
}
