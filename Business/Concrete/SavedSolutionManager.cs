using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;

namespace Business.Concrete;

public class SavedSolutionManager : ISavedSolutionService
{
    private readonly ISavedSolutionDal _savedSolutionDal;
    private readonly IWorkflowEventBus _eventBus;

    public SavedSolutionManager(ISavedSolutionDal savedSolutionDal, IWorkflowEventBus eventBus)
    {
        _savedSolutionDal = savedSolutionDal;
        _eventBus = eventBus;
    }

    public IDataResult<bool> ToggleSave(int solutionId, int userId)
    {
        var existing = _savedSolutionDal.Get(s => s.SolutionId == solutionId && s.UserId == userId);
        if (existing != null)
        {
            _savedSolutionDal.Delete(existing);

            _ = _eventBus.PublishAsync("solution.unsaved", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["SolutionId"] = solutionId
                }
            });

            return new SuccessDataResult<bool>(false, "Çözüm kaydedilenlerden çıkarıldı");
        }
        else
        {
            _savedSolutionDal.Add(new SavedSolution { SolutionId = solutionId, UserId = userId });

            _ = _eventBus.PublishAsync("solution.saved", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["SolutionId"] = solutionId
                }
            });

            return new SuccessDataResult<bool>(true, "Çözüm kaydedildi");
        }
    }

    public bool CheckSave(int solutionId, int userId)
    {
        return _savedSolutionDal.Get(s => s.SolutionId == solutionId && s.UserId == userId) != null;
    }
    
    public IDataResult<List<Entities.DTOs.SolutionDetailDto>> GetSavedSolutions(int userId)
    {
        return new SuccessDataResult<List<Entities.DTOs.SolutionDetailDto>>(_savedSolutionDal.GetSavedSolutionDetails(userId));
    }
}
