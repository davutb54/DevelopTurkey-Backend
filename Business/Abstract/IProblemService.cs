using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface IProblemService
{
    IDataResult<List<ProblemParticipantDto>> GetParticipants(int problemId);
    IDataResult<ProblemDetailDto> GetById(int id);
    IDataResult<List<Problem>> GetAll();
    IDataResult<List<ProblemDetailDto>> GetByTopic(int topicId);
    IDataResult<List<ProblemDetailDto>> GetBySender(int senderId);
    IDataResult<List<ProblemDetailDto>> GetIsHighlighted();
    IResult Add(Problem problem, List<int> topicIds);
    IResult Update(Problem problem, List<int> topicIds);
    IResult Delete(int id);
    IDataResult<List<ProblemDetailDto>> GetList(ProblemFilterDto filterDto, int institutionId);
    IDataResult<List<ProblemDetailDto>> GetReportedProblems(int? institutionId = null);
    int GetTotalCount();
    int GetReportedCount();
    IResult ReportProblem(int id);
    IResult UnReportProblem(int id);
    IResult ToggleHighlight(int id);
    IResult IncrementView(int id, string ipAddress);
    IResult ToggleResolved(int id);
    IResult ResolveProblem(int id);
    IDataResult<List<ProblemDetailDto>> GetAllForAdmin(int? institutionId = null);
    int? GetProblemInstitution(int problemId);
    IResult RemoveTopicFromProblem(int problemId, int topicId);
    IResult AssignToInstitution(int problemId, int institutionId);
    IResult SetStatus(int problemId, string status, bool value);
    IResult CloseProblem(int id, string? reason);
    IResult ReopenProblem(int id);
    IResult ToggleHide(int id);
}