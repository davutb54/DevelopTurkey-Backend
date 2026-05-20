using Core.DataAccess;
using Core.Entities.Concrete;

namespace DataAccess.Abstract;

public interface ISavedSolutionDal : IEntityRepository<SavedSolution>
{
    List<Entities.DTOs.SolutionDetailDto> GetSavedSolutionDetails(int userId);
}
