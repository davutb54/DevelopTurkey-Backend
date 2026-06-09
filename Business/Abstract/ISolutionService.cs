using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ISolutionService
{
	IDataResult<Solution?> GetById(int id);
	IDataResult<List<SolutionDetailDto>> GetAll(int institutionId);
	IDataResult<List<SolutionDetailDto>> GetByProblem(int problemId);
	IDataResult<List<SolutionDetailDto>> GetBySender(int senderId);
	IDataResult<List<SolutionDetailDto>> GetIsHighlighted();
	IResult Add(Solution solution);
	IResult Update(Solution solution);
	IResult Delete(int id);
    int GetTotalCount();
	IResult ReportSolution(int id);
	IResult UnReportSolution(int id);
	IResult ToggleHighlight(int id);
	IDataResult<List<SolutionDetailDto>> GetPendingExpertSolutions(int? institutionId = null);
	IResult ApproveSolution(int id);
	IResult RejectSolution(int id);
    IDataResult<List<SolutionDetailDto>> GetAllForAdmin(int? institutionId = null);
    int? GetSolutionInstitution(int solutionId);
}