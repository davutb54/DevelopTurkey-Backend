using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface IProblemService
{
	IDataResult<ProblemDetailDto> GetById(int id);
	IDataResult<List<Problem>> GetAll();
	IDataResult<List<ProblemDetailDto>> GetByTopic(int topicId);
	IDataResult<List<ProblemDetailDto>> GetBySender(int senderId);
	IDataResult<List<ProblemDetailDto>> GetIsHighlighted();
	IResult Add(Problem problem);
	IResult Update(Problem problem);
	IResult Delete(int id);
    IDataResult<List<ProblemDetailDto>> GetList(ProblemFilterDto filterDto);
    IDataResult<List<ProblemDetailDto>> GetReportedProblems();
    int GetTotalCount();
    int GetReportedCount();
	IResult ReportProblem(int id);
	IResult UnReportProblem(int id);
	IResult ToggleHighlight(int id);
    IResult IncrementView(int id);
	IResult ToggleResolved(int id);
	IResult ResolveProblem(int id);
}