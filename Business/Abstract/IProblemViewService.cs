using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IProblemViewService
{
    IResult RecordView(int problemId, int? userId, int institutionId);
    IDataResult<List<ProblemViewerDto>> GetViewers(int problemId);
}
