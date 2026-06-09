using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IOfficialResponseService
{
    IDataResult<List<OfficialResponseDto>> GetByProblem(int problemId);
    IResult Add(OfficialResponseAddDto dto);
    IResult UpdateStatus(int id, OfficialResponseUpdateStatusDto dto);
    IResult Delete(int id);
}
