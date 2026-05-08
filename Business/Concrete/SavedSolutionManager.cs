using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;

namespace Business.Concrete;

public class SavedSolutionManager : ISavedSolutionService
{
    private readonly ISavedSolutionDal _savedSolutionDal;

    public SavedSolutionManager(ISavedSolutionDal savedSolutionDal)
    {
        _savedSolutionDal = savedSolutionDal;
    }

    public IDataResult<bool> ToggleSave(int solutionId, int userId)
    {
        var existing = _savedSolutionDal.Get(s => s.SolutionId == solutionId && s.UserId == userId);
        if (existing != null)
        {
            _savedSolutionDal.Delete(existing);
            return new SuccessDataResult<bool>(false, "Çözüm kaydedilenlerden çıkarıldı");
        }
        else
        {
            _savedSolutionDal.Add(new SavedSolution { SolutionId = solutionId, UserId = userId });
            return new SuccessDataResult<bool>(true, "Çözüm kaydedildi");
        }
    }

    public bool CheckSave(int solutionId, int userId)
    {
        return _savedSolutionDal.Get(s => s.SolutionId == solutionId && s.UserId == userId) != null;
    }
}
