using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ICommentService
{
	IDataResult<Comment?> GetById(int id);
	IDataResult<List<CommentDetailDto>> GetAll();
	IDataResult<List<CommentDetailDto>> GetByParentCommentId(int parentCommentId);
	IDataResult<List<CommentDetailDto>> GetBySolution(int solutionId);
	IResult Add(Comment comment);
	IResult Update(Comment comment);
	IResult Delete(int id);
}