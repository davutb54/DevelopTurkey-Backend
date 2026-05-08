using Core.Utilities.Results;

namespace Business.Abstract;

public interface ISavedSolutionService
{
    IDataResult<bool> ToggleSave(int solutionId, int userId);
    bool CheckSave(int solutionId, int userId);
}
